import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient, HttpErrorResponse } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { Api, message } from './api';

describe('Sessão do Admin',()=>{
 afterEach(()=>{try{TestBed.inject(HttpTestingController).verify();}finally{TestBed.resetTestingModule();}});
 function setup(){TestBed.configureTestingModule({providers:[provideHttpClient(),provideHttpClientTesting()]});return {api:TestBed.inject(Api),http:TestBed.inject(HttpTestingController)};}
 it('obtém token CSRF antes de enviar alteração, sem expor credencial em URL',async()=>{
  // Arrange
  const {api,http}=setup();
  // Act
  const result=api.change('PUT','admin/players/id',{name:'Léo'});
  http.expectOne('/api/admin/session/csrf').flush({token:'test-csrf'});
  // Assert
  await vi.waitFor(()=>{const mutation=http.expectOne('/api/admin/players/id');expect(mutation.request.headers.get('X-VIW-CSRF')).toBe('test-csrf');expect(mutation.request.body).toEqual({name:'Léo'});mutation.flush({});});
  await result;
 });
 it('encerra o estado local quando uma alteração retorna sessão expirada',async()=>{
  // Arrange
  const {api,http}=setup();api.session.set({id:'1',email:'teste@viw.test',roles:['Editor']});
  const csrf=api.csrf();http.expectOne('/api/admin/session/csrf').flush({token:'test'});await csrf;
  // Act
  const result=api.change('POST','admin/awards',{}).catch(e=>e);
  http.expectOne('/api/admin/awards').flush({}, {status:401,statusText:'Unauthorized'});await result;
  // Assert
  expect(api.session()).toBeNull();
 });
 it('distingue conflito de edição e falta de permissão',()=>{
  // Arrange
  setup();const conflict=new HttpErrorResponse({status:409});const forbidden=new HttpErrorResponse({status:403});
  // Act
  const conflictText=message(conflict);const forbiddenText=message(forbidden);
  // Assert
  expect(conflictText).toContain('Recarregue');expect(forbiddenText).toContain('perfil');
 });
});
