import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { Api } from './api';
import { Workspace } from './workspace';

describe('Destinatários da premiação',()=>{
 afterEach(()=>TestBed.resetTestingModule());
 function setup(){
  const api={get:vi.fn()};
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
});
