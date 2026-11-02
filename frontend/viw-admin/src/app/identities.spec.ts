import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { Api, Source, Edition } from './api';
import { Identities } from './identities';
import { Positions } from './positions';
describe('Identidades e posições por copa',()=>{
 afterEach(()=>TestBed.resetTestingModule());
 function setup(){const api={get:vi.fn().mockResolvedValue({items:[],totalCount:0}),change:vi.fn().mockResolvedValue({})};TestBed.configureTestingModule({providers:[{provide:Api,useValue:api}]});return {api,wizard:TestBed.runInInjectionContext(()=>new Identities())};}
 const source=(id:string,cup:string):Source=>({id,editionId:cup,name:id,playerId:null,playerName:null,linkId:null,position:null,version:'version'});
 it('começa com cadastro escolhido e percorre somente outras copas',async()=>{
  const {wizard}=setup();wizard.editions.set([{id:'a'},{id:'b'},{id:'c'}] as Edition[]);await wizard.start(source('Felipinho','a'));
  expect(wizard.currentCup()).toBe('b');expect(wizard.selected().length).toBe(1);
  wizard.choose(source('Meireles','b'));wizard.choose(source('Outro','b'));
  expect(wizard.selected().map(s=>s.id)).toEqual(['Felipinho','Outro']);
 });
 it('cria pessoa com um cadastro sem pedir motivo',async()=>{
  const {wizard,api}=setup();wizard.selected.set([source('um','a')]);wizard.name='Nome final';await wizard.save();
  expect(api.change).toHaveBeenCalledWith('POST','admin/identities',{name:'Nome final',sourceIds:['um'],version:null});
 });
 it('filtra goleiros pela copa e equipe',async()=>{
  const {api}=setup();const positions=TestBed.runInInjectionContext(()=>new Positions());positions.cup='copa';positions.team='equipe';positions.goalkeepers=true;await positions.load();
  expect(api.get).toHaveBeenCalledWith('admin/sources?editionId=copa&page=1&goalkeepers=true&search=&teamId=equipe');
 });
});
