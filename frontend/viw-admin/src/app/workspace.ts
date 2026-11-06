import { Component, inject, signal, OnInit, OnDestroy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DATE_PIPE_DEFAULT_OPTIONS, DatePipe, formatDate, JsonPipe } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { Subscription } from 'rxjs';
import { PlayerPhoto } from './player-photo';
import { ViwDataTable } from './data-table';
import { Api, message, Page, Person, Source, Definition, Edition, Award, Assignment, Audit, Account, Choice } from './api';
@Component({ selector: 'viw-workspace', standalone: true, imports: [FormsModule, DatePipe, JsonPipe, PlayerPhoto, ViwDataTable], providers: [{ provide: DATE_PIPE_DEFAULT_OPTIONS, useValue: { timezone: '-0300' } }], templateUrl: './workspace.html' })
export class Workspace implements OnInit, OnDestroy {
    readonly timestamp = Date.parse;
    api = inject(Api); route = inject(ActivatedRoute); private subscription?: Subscription; private generation = 0; private targetGeneration = 0;
    section = signal('players'); busy = signal(false); error = signal(''); notice = signal(''); page = signal(1); total = signal(0);
    people = signal<Person[]>([]); sources = signal<Source[]>([]); awards = signal<Award[]>([]); assignments = signal<Assignment[]>([]); audit = signal<Audit[]>([]); accounts = signal<Account[]>([]);
    editions = signal<Edition[]>([]); definitions = signal<Definition[]>([]); teams = signal<Choice[]>([]); rounds = signal<Choice[]>([]); matches = signal<Choice[]>([]);
    search = ''; editionId = ''; targetSearch = ''; targets = signal<Person[]>([]); target: Person | null = null; source: Source | null = null; person: Person | null = null;
    personForm = { name: '', slug: '', reason: '' }; reason = ''; editingId: string | null = null; version = '';
    awardForm = { definitionId: '', matchId: '', roundId: '', position: '', status: 'Draft' };
    assignmentForm = { teamId: '', coachId: '', roundId: '', startsAt: '', endsAt: '', reason: '' };
    accountForm = { email: '', password: '', role: 'Editor' }; passwordForm = { currentPassword: '', newPassword: '' }; securityEvents = signal<unknown[]>([]);
    titles: Record<string, string> = { players: 'Jogadores', identities: 'Unir jogadores', awards: 'Prêmios e seleções', coaches: 'Técnicos', assignments: 'Equipes e técnicos', audit: 'Histórico de alterações', accounts: 'Contas e acessos', password: 'Minha senha' };
    async ngOnInit() {
        this.subscription = this.route.data.subscribe(data => { this.generation++; this.section.set(data['section']); this.page.set(1); this.search = ''; this.clear(); void this.load(); });
        try { this.editions.set(await this.api.all<Edition>('v1/championships')); this.definitions.set(await this.api.get<Definition[]>('admin/award-definitions')); } catch (e) { this.error.set(message(e)); }
    }
    ngOnDestroy() { this.subscription?.unsubscribe(); this.generation++; }
    clear() { this.targetGeneration++; this.targetSearch = ''; this.person = null; this.source = null; this.target = null; this.targets.set([]); this.editingId = null; this.version = ''; this.reason = ''; this.error.set(''); this.notice.set(''); this.awardForm = { definitionId: '', matchId: '', roundId: '', position: '', status: 'Draft' }; this.assignmentForm = { teamId: '', coachId: '', roundId: '', startsAt: '', endsAt: '', reason: '' }; }
    async load() {
        const generation = ++this.generation; const section = this.section(); this.busy.set(true); this.error.set('');
        try {
            const q = '?search=' + encodeURIComponent(this.search);
            if (section === 'players' || section === 'coaches') { const rows = await this.api.all<Person>('admin/' + section + q); if (generation !== this.generation) return; this.people.set(rows); }
            if (section === 'identities') { const rows = await this.api.all<Source>('admin/sources' + q + (this.editionId ? '&editionId=' + this.editionId : '')); if (generation !== this.generation) return; this.sources.set(rows); }
            if (section === 'awards' || section === 'assignments') {
                if (!this.editionId) { this.awards.set([]); this.assignments.set([]); return; }
                const path = 'admin/' + section + '?editionId=' + this.editionId;
                if (section === 'awards') { const rows = await this.api.all<Award>(path); if (generation !== this.generation) return; this.awards.set(rows); }
                else { const rows = await this.api.all<Assignment>(path); if (generation !== this.generation) return; this.assignments.set(rows); }
            }
            if (section === 'audit') { const p = await this.api.get<Page<Audit>>('admin/audit?page=' + this.page()); if (generation !== this.generation) return; this.audit.set(p.items); this.total.set(p.totalCount); }
            if (section === 'accounts') { const rows = await this.api.all<Account>('admin/users'); if (generation !== this.generation) return; this.accounts.set(rows); const events = await this.api.get<Page<unknown>>('admin/users/events'); if (generation === this.generation) this.securityEvents.set(events.items); }
        } catch (e) { if (generation === this.generation) this.error.set(message(e)); } finally { if (generation === this.generation) this.busy.set(false); }
    }
    async filter() { this.page.set(1); this.clear(); await this.load(); }
    async paginate(delta: number) { this.page.update(x => x + delta); this.clear(); await this.load(); }
    async editionChanged() {
        this.clear(); this.page.set(1); const edition = this.editions().find(x => x.id === this.editionId); this.teams.set([]); this.rounds.set([]); this.matches.set([]); await this.load(); if (!edition) return; try {
            const detail = await this.api.get<{ rounds: Choice[] }>('v1/championships/' + edition.slug); this.rounds.set(detail.rounds);
            this.teams.set(await this.api.all<Choice>('v1/championships/' + edition.slug + '/teams'));
            const games = await this.api.all<{ id: string; home: { name: string } | null; away: { name: string } | null; scheduledAt: string | null }>('v1/championships/' + edition.slug + '/matches');
            this.matches.set(games.map(x => ({ id: x.id, name: (x.home?.name ?? 'A definir') + ' × ' + (x.away?.name ?? 'A definir') + (x.scheduledAt ? ' · ' + formatDate(x.scheduledAt, 'dd/MM/yyyy', 'en-US', '-0300') : '') + ' · ' + x.id.slice(0, 8) })));
        } catch (e) { this.error.set(message(e)); }
    }
    async run(action: () => Promise<unknown>, success: string) { if (this.busy()) return; this.busy.set(true); this.error.set(''); this.notice.set(''); try { await action(); this.clear(); await this.load(); this.notice.set(success); } catch (e) { this.error.set(message(e)); } finally { this.busy.set(false); } }
    editPerson(p: Person) { this.clear(); this.person = p; this.personForm = { name: p.name, slug: p.slug, reason: '' }; }
    savePerson() { if (!this.person) return; const p = this.person; return this.run(() => this.api.change('PUT', 'admin/' + this.section() + '/' + p.id, { ...this.personForm, imageId: p.imageId, version: p.version }), 'Cadastro atualizado e auditado.'); }
    newCoach() { this.clear(); this.personForm = { name: '', slug: '', reason: '' }; this.editingId = 'new'; }
    createCoach() { return this.run(() => this.api.change('POST', 'admin/coaches', this.personForm), 'Técnico cadastrado.'); }
    async findTargets(coaches = false) {
        const request = ++this.targetGeneration; const edition = this.editionId; const scoped = this.section() === 'awards' && !coaches;
        this.error.set(''); this.targets.set([]); this.target = null;
        if (scoped && !edition) return;
        try {
            if(scoped){const p=await this.api.get<Page<Source>>('admin/sources?editionId='+edition+'&search='+encodeURIComponent(this.targetSearch));
                if(request===this.targetGeneration && edition===this.editionId)this.targets.set(p.items.map(x=>({id:x.id,name:x.name,slug:'',imageId:null,version:x.version})));return;}
            const p = await this.api.get<Page<Person>>('admin/' + (coaches ? 'coaches' : 'players') + '?search=' + encodeURIComponent(this.targetSearch) + (scoped ? '&editionId=' + encodeURIComponent(edition) : ''));
            if (request !== this.targetGeneration || edition !== this.editionId) return; this.targets.set(p.items);
        } catch (e) { if (request === this.targetGeneration) this.error.set(message(e)); }
    }
    chooseSource(s: Source) { this.clear(); this.source = s; this.targetSearch = ''; }
    link() { if (!this.source || !this.target) return; const s = this.source, t = this.target; return this.run(() => this.api.change('PUT', 'admin/sources/' + s.id + '/identity', { playerId: t.id, expectedLinkId: s.linkId, reason: this.reason }), 'Identidade atualizada. O vínculo anterior foi preservado no histórico.'); }
    definition() { return this.definitions().find(x => x.id === this.awardForm.definitionId); }
    changeDefinition() { this.target = null; this.targets.set([]); this.awardForm.matchId = ''; this.awardForm.roundId = ''; }
    async editAward(a: Award) {
        this.clear(); this.editingId = a.id; this.version = a.version; this.awardForm = { definitionId: a.definitionId, matchId: a.matchId ?? '', roundId: a.roundId ?? '', position: a.position ?? '', status: a.status };
        if (a.sourcePlayerId) { this.target = {id:a.sourcePlayerId,name:a.recipientName??'Jogador',slug:'',imageId:null,version:''}; }
        else if (a.coachId) { this.targetSearch = ''; await this.findTargets(true); this.target = this.targets().find(x => x.id === a.coachId) ?? { id: a.coachId, name: a.recipientName ?? 'Técnico selecionado', slug: '', imageId: null, version: '' }; }
    }
    saveAward() { if (!this.target) return; const c = { ...this.awardForm, editionId: this.editionId, matchId: this.awardForm.matchId || null, roundId: this.awardForm.roundId || null, sourcePlayerId: this.definition()?.recipientKind === 'Player' ? this.target.id : null, coachId: this.definition()?.recipientKind === 'Coach' ? this.target.id : null, position: this.awardForm.position || null, version: this.version || null }; return this.run(() => this.api.change(this.editingId ? 'PUT' : 'POST', 'admin/awards' + (this.editingId ? '/' + this.editingId : ''), c), 'Premiação salva. Apenas prêmios confirmados aparecem no portal.'); }
    async editAssignment(a: Assignment) { this.clear(); this.editingId = a.id; this.version = a.version; this.assignmentForm = { teamId: a.teamId, coachId: a.coachId, roundId: a.roundId ?? '', startsAt: this.localDate(a.startsAt), endsAt: a.endsAt ? this.localDate(a.endsAt) : '', reason: '' }; this.target = { id: a.coachId, name: a.coachName ?? 'Técnico designado', slug: '', imageId: null, version: '' }; }
    localDate(value: string) { return formatDate(value, "yyyy-MM-dd'T'HH:mm", 'en-US', '-0300'); }
    saveAssignment() { const c = { ...this.assignmentForm, editionId: this.editionId, coachId: this.target?.id, roundId: this.assignmentForm.roundId || null, startsAt: new Date(this.assignmentForm.startsAt + '-03:00').toISOString(), endsAt: this.assignmentForm.endsAt ? new Date(this.assignmentForm.endsAt + '-03:00').toISOString() : null, version: this.version || null }; return this.run(() => this.api.change(this.editingId ? 'PUT' : 'POST', 'admin/assignments' + (this.editingId ? '/' + this.editingId : ''), c), 'Designação salva.'); }
    createAccount() { return this.run(async () => { await this.api.change('POST', 'admin/users', this.accountForm); this.accountForm = { email: '', password: '', role: 'Editor' }; }, 'Conta criada. Compartilhe a credencial por um canal seguro.'); }
    changeAccess(a: Account) { return this.run(() => this.api.change('PUT', 'admin/users/' + a.id + '/access', { enabled: !a.enabled }), 'Acesso atualizado; sessões anteriores invalidadas.'); }
    changePassword() { return this.run(async () => { await this.api.change('POST', 'admin/session/password', this.passwordForm); this.passwordForm = { currentPassword: '', newPassword: '' }; await this.api.csrf(); }, 'Senha alterada.'); }
    editionName(id: string) { const e = this.editions().find(x => x.id === id); return e ? e.name + ' · ' + e.year + '/' + e.semester : id; }
    categoryName(id: string) { return this.definitions().find(x => x.id === id)?.name ?? id; }
    teamName(id: string) { return this.teams().find(x => x.id === id)?.name ?? id; }
}
