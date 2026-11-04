import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { Api } from './api';
import { Workspace } from './workspace';

describe('Destinatários da premiação',()=>{
 afterEach(()=>TestBed.resetTestingModule());
 function setup(){
  const api={get:vi.fn(),all:vi.fn()};
  TestBed.configureTestingModule({providers:[{provide:Api,useValue:api},{provide:ActivatedRoute,useValue:{}}]});
  const workspace=TestBed.runInInjectionContext(()=>new Workspace());
  return {workspace,api};
 }
 it('busca apenas jogadores da edição da premiação',async()=>{
  const {workspace,api}=setup();workspace.section.set('awards');workspace.editionId='copa-mundo';workspace.targetSearch='Pimenta';api.get.mockResolvedValue({items:[]});
  await workspace.findTargets();
  expect(api.get).toHaveBeenCalledWith('admin/sources?editionId=copa-mundo&search=Pimenta');
 });
 it('mantém busca global para unir identidades de campeonatos diferentes',async()=>{
  const {workspace,api}=setup();workspace.section.set('identities');workspace.editionId='copa-mundo';workspace.targetSearch='Edvaldo';api.get.mockResolvedValue({items:[]});
  await workspace.findTargets();
  expect(api.get).toHaveBeenCalledWith('admin/players?search=Edvaldo');
 });
 it('descarta resultado antigo quando o campeonato muda',async()=>{
  const {workspace,api}=setup();workspace.section.set('awards');workspace.editionId='antiga';
  let resolve!:(value:unknown)=>void;api.get.mockReturnValue(new Promise(r=>resolve=r));
  const request=workspace.findTargets();workspace.editionId='nova';workspace.clear();resolve({items:[{id:'jogador-antigo'}]});await request;
  expect(workspace.targets()).toEqual([]);expect(workspace.target).toBeNull();
 });
 it('não busca destinatário sem campeonato selecionado',async()=>{
  const {workspace,api}=setup();workspace.section.set('awards');await workspace.findTargets();expect(api.get).not.toHaveBeenCalled();
 });
 it.each([
  ['players','admin/players?search=Nome'],
  ['coaches','admin/coaches?search=Nome'],
  ['identities','admin/sources?search=Nome&editionId=copa'],
  ['awards','admin/awards?editionId=copa'],
  ['assignments','admin/assignments?editionId=copa'],
  ['accounts','admin/users']
 ] as const)('carrega o conjunto completo da tabela %s mantendo os filtros',async(section,path)=>{
  const {workspace,api}=setup();workspace.section.set(section);workspace.search='Nome';workspace.editionId='copa';
  const rows=Array.from({length:45},(_,id)=>({id:String(id)}));
  api.all.mockResolvedValue(rows);api.get.mockResolvedValue({items:[]});
  await workspace.load();
  expect(api.all).toHaveBeenCalledWith(path);
  const grids={players:workspace.people,coaches:workspace.people,identities:workspace.sources,awards:workspace.awards,assignments:workspace.assignments,accounts:workspace.accounts};
  expect(grids[section]()).toEqual(rows);
  expect(workspace.error()).toBe('');
 });
 it('não substitui a tabela nova pelo resultado de uma consulta anterior',async()=>{
  const {workspace,api}=setup();let resolve!:(value:unknown)=>void;
  api.all.mockReturnValueOnce(new Promise(r=>resolve=r)).mockResolvedValueOnce([{id:'novo'}]);
  const previous=workspace.load();workspace.section.set('coaches');await workspace.load();
  resolve([{id:'antigo'}]);await previous;
  expect(workspace.people()).toEqual([{id:'novo'}]);
  expect(workspace.busy()).toBe(false);
 });
});
