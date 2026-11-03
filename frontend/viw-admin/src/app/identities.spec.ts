import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { Api, Source, Edition } from './api';
import { Identities } from './identities';
import { Positions } from './positions';
describe('Identidades e posições por copa', () => {
    afterEach(() => TestBed.resetTestingModule());
    function setup() { const api = { all: vi.fn().mockResolvedValue([]), get: vi.fn().mockResolvedValue({ items: [], totalCount: 0 }), change: vi.fn().mockResolvedValue({}) }; TestBed.configureTestingModule({ providers: [{ provide: Api, useValue: api }] }); return { api, wizard: TestBed.runInInjectionContext(() => new Identities()) }; }
    const source = (id: string, cup: string): Source => ({ id, editionId: cup, name: id, playerId: null, playerName: null, linkId: null, position: null, version: 'version' });
    it('começa com cadastro escolhido e percorre somente outras copas', async () => {
        const { wizard } = setup(); wizard.editions.set([{ id: 'a' }, { id: 'b' }, { id: 'c' }] as Edition[]); await wizard.start(source('Felipinho', 'a'));
        expect(wizard.currentCup()).toBe('b'); expect(wizard.selected().length).toBe(1);
        wizard.choose(source('Meireles', 'b')); wizard.choose(source('Outro', 'b'));
        expect(wizard.selected().map(s => s.id)).toEqual(['Felipinho', 'Outro']);
    });
    it('cria pessoa com um cadastro sem pedir motivo', async () => {
        const { wizard, api } = setup(); wizard.selected.set([source('um', 'a')]); wizard.name = 'Nome final'; await wizard.save();
        expect(api.change).toHaveBeenCalledWith('POST', 'admin/identities', { name: 'Nome final', sourceIds: ['um'], version: null });
    });
    it('filtra cadastros sem Player por equipe e copa mesmo sem nome', async () => {
        const { wizard, api } = setup(); wizard.cup = 'copa'; wizard.team = 'equipe'; wizard.unlinkedOnly = true; await wizard.load();
        expect(api.get).toHaveBeenCalledWith('admin/sources?page=1&search=&editionId=copa&teamId=equipe&unlinkedOnly=true');
    });
    it('filtra candidatos pelo time e limpa o time ao mudar de copa', async () => {
        const { wizard, api } = setup(); wizard.steps = [{ id: 'a' }, { id: 'b' }] as Edition[]; wizard.candidateTeam = 'equipe'; await wizard.candidates();
        expect(api.get).toHaveBeenLastCalledWith('admin/sources?editionId=a&search=&page=1&teamId=equipe');
        await wizard.move(1); expect(wizard.candidateTeam).toBe('');
        expect(api.get).toHaveBeenLastCalledWith('admin/sources?editionId=b&search=&page=1');
    });
    it('filtra goleiros pela copa e equipe', async () => {
        const { api } = setup(); const positions = TestBed.runInInjectionContext(() => new Positions()); positions.cup = 'copa'; positions.team = 'equipe'; positions.goalkeepers = true; await positions.load();
        expect(api.get).toHaveBeenCalledWith('admin/sources?editionId=copa&page=1&goalkeepers=true&search=&teamId=equipe');
    });
    it('preserva a busca composta na API e não une candidatos automaticamente', async () => {
        const { wizard, api } = setup(); wizard.steps = [{ id: 'a' }] as Edition[]; wizard.candidateSearch = 'Edvaldo Farias';
        api.get.mockResolvedValue({ items: [source('E. Santana', 'a')], totalCount: 1 });
        await wizard.candidates();
        expect(api.get).toHaveBeenLastCalledWith('admin/sources?editionId=a&search=Edvaldo%20Farias&page=1');
        expect(wizard.selected()).toEqual([]);
    });
    it('mostra as equipes e um retrato maior em cada candidato', () => {
        const { api } = setup(); const fixture = TestBed.createComponent(Identities); const wizard = fixture.componentInstance;
        wizard.active.set(true); wizard.steps = [{ id: 'a' }] as Edition[];
        wizard.choices.set([{ ...source('Edvaldo', 'a'), teams: [{ id: 'home', name: 'Barcelona' }, { id: 'away', name: 'Aurora' }] }]);
        fixture.detectChanges();
        const card = fixture.nativeElement.querySelector('.choices button') as HTMLButtonElement;
        expect(card.textContent).toContain('Barcelona · Aurora');
        expect(card.querySelector('.admin-photo-large')).not.toBeNull();
        expect(wizard.teamNames(source('Sem elenco', 'a'))).toBe('Equipe não registrada');
        expect(api.change).not.toHaveBeenCalled();
    });
    it('explica a busca sem acentos e aproximada sem confirmar identidade automaticamente', () => {
        setup(); const fixture = TestBed.createComponent(Identities);
        fixture.componentInstance.active.set(true);
        fixture.componentInstance.steps = [{ id: 'a' }] as Edition[];
        fixture.detectChanges();
        const hint = fixture.nativeElement.querySelector('.hint') as HTMLElement;
        expect(hint.textContent).toContain('ignora acentos');
        expect(hint.textContent).toContain('pequenas diferenças de escrita');
        expect(hint.textContent).toContain('Nomes exatos aparecem primeiro');
        expect(hint.textContent).toContain('não confirmam a mesma pessoa');
    });
    it('mostra a foto e identificação da base acima da busca e dos candidatos', async () => {
        const { api } = setup();
        api.all.mockResolvedValue([{ id: 'a', name: 'EuroViw', year: 2024, semester: 2 }, { id: 'b', name: 'Copa VIW', year: 2025, semester: 1 }]);
        const fixture = TestBed.createComponent(Identities);
        fixture.detectChanges(); await fixture.whenStable();
        const wizard = fixture.componentInstance;
        const base = { ...source('base', 'a'), name: 'ALEF', teams: [{ id: 'uk', name: 'UCRÂNIA' }] };
        await wizard.start(base);
        wizard.choices.set([source('candidato', 'b')]);
        fixture.detectChanges();

        const reference = fixture.nativeElement.querySelector('.comparison-base') as HTMLElement;
        expect(reference.textContent).toContain('Jogador base de comparação');
        expect(reference.textContent).toContain('ALEF');
        expect(reference.textContent).toContain('EuroViw · 2024/2');
        expect(reference.textContent).toContain('UCRÂNIA');
        expect(reference.querySelector('.admin-photo-large')).not.toBeNull();
        expect(reference.querySelector('img')?.getAttribute('src')).toBe('/api/media/sourceplayers/base');
        for (const selector of ['form', '.choices']) {
            expect(reference.compareDocumentPosition(fixture.nativeElement.querySelector(selector)) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
        }
    });
    it('mantém a base original ao trocar copa, candidatos e seleção e limpa ao fechar o assistente', async () => {
        const { wizard } = setup();
        wizard.editions.set([{ id: 'a' }, { id: 'b' }, { id: 'c' }] as Edition[]);
        const base = source('base', 'a');
        await wizard.start(base);
        wizard.choose(source('outro', 'b'));
        await wizard.move(1);
        wizard.remove(base.id);
        wizard.choicePage = 2;
        await wizard.candidates(false);
        expect(wizard.basePlayer()).toBe(base);
        expect(wizard.selected().map(s => s.id)).toEqual(['outro']);

        wizard.cancel();
        expect(wizard.basePlayer()).toBeNull();
        const next = source('nova-base', 'b');
        await wizard.start(next);
        expect(wizard.basePlayer()).toBe(next);
    });
    it('usa na edição o cadastro clicado como base, não o primeiro vínculo retornado', async () => {
        const { wizard, api } = setup();
        wizard.editions.set([{ id: 'a' }, { id: 'b' }] as Edition[]);
        const clicked = { ...source('base', 'b'), playerId: 'pessoa' };
        const refreshed = { ...clicked, teams: [{ id: 'uk', name: 'UCRÂNIA' }] };
        api.get.mockResolvedValueOnce({ player: { name: 'ALEF', version: 'atual' }, sources: [source('primeiro', 'a'), refreshed] });

        await wizard.start(clicked);

        expect(wizard.basePlayer()).toBe(refreshed);
        expect(wizard.selected().length).toBe(2);
        expect(wizard.version).toBe('atual');
    });
});
