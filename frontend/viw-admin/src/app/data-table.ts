import { afterEveryRender, Directive, ElementRef, HostListener, inject, Input, OnChanges, OnDestroy } from '@angular/core';
import DataTable, { Api, Config, Order } from 'datatables.net-bs5';
import options from '../../../shared/table-options.json';

DataTable.ext.errMode = 'throw';

@Directive({ selector: 'table[viwDataTable]', standalone: true })
export class ViwDataTable implements OnChanges, OnDestroy {
    @Input({ required: true }) viwDataTable: readonly unknown[] = [];
    private readonly table = inject<ElementRef<HTMLTableElement>>(ElementRef).nativeElement;
    private instance?: Api<unknown>;
    private bodyNodes: ChildNode[] = [];
    private pending = true;
    private dirty = false;
    private state?: { order: Order; search: Config['search']; length: number; start: number };

    constructor() {
        afterEveryRender(() => {
            if (this.pending) {
                this.pending = false;
                const state = this.state;
                const length = state?.length ?? options.pageLength;
                const lastStart = Math.max(0, Math.ceil(this.viwDataTable.length / length) - 1) * length;
                this.bodyNodes = Array.from(this.table.tBodies[0].childNodes);
                this.instance = new DataTable(this.table, {
                    ...options,
                    order: state?.order ?? [],
                    search: state?.search ?? { search: '' },
                    pageLength: length,
                    displayStart: Math.min(state?.start ?? 0, lastStart),
                    columnDefs: [{ targets: '[data-dt-order="disable"]', orderable: false, searchable: false }]
                });
                this.dirty = false;
            } else if (this.dirty && this.instance) {
                this.dirty = false;
                this.instance.rows().invalidate('dom').draw(false);
            }
        });
    }

    ngOnChanges() {
        if (this.instance) {
            const page = this.instance.page.info();
            this.state = { order: this.instance.order(), search: { search: this.instance.search() }, length: page.length, start: page.start };
            this.release();
        }
        this.pending = true;
    }

    @HostListener('change')
    refreshCells() { this.dirty = true; }

    private release() {
        this.instance?.destroy();
        // DataTables drops @for's comment anchors; restore them before Angular updates or removes rows.
        this.table.tBodies[0].replaceChildren(...this.bodyNodes);
        this.instance = undefined;
    }

    ngOnDestroy() { if (this.instance) this.release(); }
}
