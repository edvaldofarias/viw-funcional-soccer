import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import DataTable from 'datatables.net-bs5';
import { ViwDataTable } from './data-table';

@Component({
    standalone: true,
    imports: [FormsModule, ViwDataTable],
    template: `@if(visible()){
        <table [viwDataTable]="rows()">
            <thead><tr><th>Nome</th><th>Gols</th><th>Posição</th><th data-dt-order="disable">Ações</th></tr></thead>
            <tbody>@for(row of rows();track row.id){
                <tr [attr.data-id]="row.id">
                    <td [attr.data-order]="row.name" [attr.data-search]="row.name"><span aria-hidden="true">ZZ</span>{{row.name}}</td>
                    <td [attr.data-order]="row.goals">{{row.goals}}</td>
                    <td [attr.data-order]="row.position" [attr.data-search]="row.position">
                        <select [(ngModel)]="row.position"><option>Atacante</option><option>Goleiro</option><option>Zagueiro</option></select>
                    </td>
                    <td><button (click)="selected=row.id">Editar</button></td>
                </tr>
            }</tbody>
        </table>
    }`
})
class TableHost {
    visible = signal(true);
    rows = signal(Array.from({ length: 35 }, (_, id) => ({
        id, name: id === 34 ? 'Álvaro' : 'Jogador ' + id, goals: id === 34 ? 99 : id, position: 'Goleiro'
    })));
    selected = -1;
}

describe('DataTables com linhas Angular', () => {
    afterEach(() => TestBed.resetTestingModule());
    async function setup() {
        TestBed.configureTestingModule({ imports: [TableHost] });
        const fixture = TestBed.createComponent(TableHost);
        fixture.detectChanges();
        await fixture.whenStable();
        const table = fixture.nativeElement.querySelector('table') as HTMLTableElement;
        const api = new DataTable.Api(table);
        return { fixture, host: fixture.componentInstance, table, api };
    }

    it('mantém a ordem inicial e ordena numericamente todo o conjunto, além da primeira página', async () => {
        const { table, api } = await setup();
        expect(api.page.info().recordsTotal).toBe(35);
        expect(table.tBodies[0].rows.length).toBe(20);
        expect(table.tBodies[0].rows[0].dataset['id']).toBe('0');
        api.order([1, 'desc']).draw();
        const goals = Array.from(table.tBodies[0].rows, row => Number(row.cells[1].textContent));
        expect(goals[0]).toBe(99);
        expect(goals).toEqual([...goals].sort((a, b) => b - a));
        api.order([1, 'asc']).draw();
        expect(table.tBodies[0].rows[2].cells[1].textContent).toBe('2');
        expect(table.tBodies[0].rows[10].cells[1].textContent).toBe('10');
        expect(table.tHead?.rows[0].cells[3].querySelector('[role=button]')).toBeNull();
    });

    it('busca nomes sem incluir o monograma e ignora acentos', async () => {
        const { api, table } = await setup();
        api.search('alvaro').draw();
        expect(api.page.info().recordsDisplay).toBe(1);
        expect(table.tBodies[0].rows[0].dataset['id']).toBe('34');
        api.search('ZZ').draw();
        expect(api.page.info().recordsDisplay).toBe(0);
    });

    it('preserva ações, ordenação e tamanho de página ao atualizar linhas rastreadas', async () => {
        const { fixture, host, table, api } = await setup();
        api.order([1, 'desc']).page.len(10).draw();
        api.page(1).draw(false);
        table.tBodies[0].rows[0].querySelector('button')!.click();
        expect(host.selected).toBe(Number(table.tBodies[0].rows[0].dataset['id']));
        host.rows.set([
            { id: 2, name: 'Atualizado', goals: 120, position: 'Atacante' },
            { id: 40, name: 'Novo', goals: 10, position: 'Goleiro' }
        ]);
        fixture.detectChanges();
        await fixture.whenStable();
        const updated = new DataTable.Api(table);
        expect(updated.page.len()).toBe(10);
        expect(updated.page()).toBe(0);
        expect(updated.order().map(([column, direction]) => [column, direction])).toEqual([[1, 'desc']]);
        expect(updated.rows().count()).toBe(2);
        expect(fixture.nativeElement.querySelectorAll('.dt-container').length).toBe(1);
        expect(table.tBodies[0].rows[0].textContent).toContain('Atualizado');
        table.tBodies[0].rows[0].querySelector('button')!.click();
        expect(host.selected).toBe(2);
    });

    it('atualiza o cache de ordenação e busca ao editar uma posição', async () => {
        const { fixture, host, table, api } = await setup();
        api.order([2, 'asc']).draw();
        const select = table.tBodies[0].rows[0].querySelector('select')!;
        select.value = 'Zagueiro';
        select.dispatchEvent(new Event('change', { bubbles: true }));
        fixture.detectChanges();
        await fixture.whenStable();
        expect(host.rows()[0].position).toBe('Zagueiro');
        api.search('Zagueiro').draw();
        expect(api.page.info().recordsDisplay).toBe(1);
        expect(table.tBodies[0].rows[0].dataset['id']).toBe('0');
        api.search('Atacante').draw();
        expect(api.page.info().recordsDisplay).toBe(0);
    });

    it('mantém a busca e ajusta a página quando uma recarga reduz os resultados filtrados', async () => {
        const { fixture, host, table, api } = await setup();
        api.search('Jogador').draw();
        api.page(1).draw(false);
        host.rows.set(Array.from({ length: 50 }, (_, id) => ({
            id, name: id === 0 ? 'Jogador restante' : 'Outro ' + id, goals: id, position: 'Goleiro'
        })));
        fixture.detectChanges();
        await fixture.whenStable();
        const updated = new DataTable.Api(table);
        expect(updated.search()).toBe('Jogador');
        expect(updated.page.info().recordsDisplay).toBe(1);
        expect(updated.page()).toBe(0);
        expect(table.tBodies[0].rows[0].textContent).toContain('Jogador restante');
    });

    it('aceita vazio, recarga e saída de tela sem duplicar instâncias ou deixar linhas antigas', async () => {
        const { fixture, host, table } = await setup();
        host.rows.set([]);
        fixture.detectChanges();
        await fixture.whenStable();
        expect(new DataTable.Api(table).rows().count()).toBe(0);
        expect(table.tBodies[0].textContent).toContain('Nenhum registro encontrado.');
        host.rows.set([{ id: 1, name: 'Retorno', goals: 2, position: 'Atacante' }]);
        fixture.detectChanges();
        await fixture.whenStable();
        expect(new DataTable.Api(table).rows().count()).toBe(1);
        expect(table.tBodies[0].textContent).toContain('Retorno');
        host.visible.set(false);
        fixture.detectChanges();
        expect(DataTable.isDataTable(table)).toBe(false);
        expect(fixture.nativeElement.querySelector('.dt-container')).toBeNull();
    });
});
