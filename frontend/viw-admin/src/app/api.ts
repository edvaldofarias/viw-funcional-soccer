import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
export interface Page<T> {items:T[];page:number;totalCount:number;}
export interface Person {id:string;name:string;slug:string;imageId:string|null;version:string;championships?:string[];}
export interface Source {id:string;editionId:string;name:string;playerId:string|null;playerName:string|null;linkId:string|null;}
export interface Definition {id:string;code:string;name:string;scope:string;recipientKind:string;}
export interface Edition {id:string;slug:string;name:string;year:number;semester:number;}
export interface Award {id:string;editionId:string;definitionId:string;matchId:string|null;roundId:string|null;playerId:string|null;coachId:string|null;position:string|null;status:string;version:string;recipientName:string|null;}
export interface Assignment {id:string;editionId:string;teamId:string;coachId:string;roundId:string|null;startsAt:string;endsAt:string|null;version:string;coachName:string|null;}
export interface Session {id:string;email:string;roles:string[];}
export interface Audit {id:string;entityType:string;entityId:string;actor:string;reason:string;beforeJson:string|null;afterJson:string|null;occurredAt:string;}
export interface Account {id:string;email:string;roles:string[];enabled:boolean;}
export interface Choice {id:string;name:string;}
@Injectable({providedIn:'root'})
export class Api {
 private http=inject(HttpClient);private token='';session=signal<Session|null>(null);
 async get<T>(path:string){try{return await firstValueFrom(this.http.get<T>('/api/'+path));}catch(error){if(error instanceof HttpErrorResponse && error.status===401){this.session.set(null);this.token='';}throw error;}}
 async csrf(){this.token=(await this.get<{token:string}>('admin/session/csrf')).token;}
 async change<T>(method:string,path:string,body:unknown){
   if(!this.token)await this.csrf();
   try{return await firstValueFrom(this.http.request<T>(method,'/api/'+path,{body,headers:{'X-VIW-CSRF':this.token}}));}
   catch(error){if(error instanceof HttpErrorResponse && error.status===401){this.session.set(null);this.token='';}throw error;}
 }
 async restore(){try{this.session.set(await this.get<Session>('admin/session'));await this.csrf();}catch{this.session.set(null);}}
 async login(email:string,password:string){await this.csrf();await this.change('POST','admin/session/login',{email,password});this.session.set(await this.get<Session>('admin/session'));await this.csrf();}
 async logout(){await this.change('POST','admin/session/logout',{});this.session.set(null);this.token='';}
 async all<T>(path:string):Promise<T[]>{
   const values:T[]=[];for(let page=1;page<=1000;page++){const result=await this.get<Page<T>>(path+(path.includes('?')?'&':'?')+'page='+page+'&pageSize=100');values.push(...result.items);if(values.length>=result.totalCount)return values;if(!result.items.length)throw new Error('Consulta incompleta.');}throw new Error('Consulta muito grande; refine a edição.');
 }
}
export function message(error:unknown):string {
 if(error instanceof HttpErrorResponse){if(error.status===401)return 'Sua sessão expirou. Entre novamente.';if(error.status===403)return 'Seu perfil não permite esta ação.';if(error.status===409)return 'O registro foi alterado ou conflita com outro cadastro. Recarregue antes de salvar.';if(error.status===429)return 'Muitas tentativas. Aguarde um minuto.';if(error.status===400)return error.error?.message??error.error?.title??'Confira os campos. Se a sessão mudou, recarregue a página.';return 'Serviço indisponível. Tente novamente em instantes.';}
 return 'Não foi possível concluir. Tente novamente.';
}
