import { Component, inject, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { Api, message } from './api';
@Component({selector:'viw-admin',standalone:true,imports:[FormsModule,RouterLink,RouterLinkActive,RouterOutlet],template:`
@if(loading()){<main class="loading" aria-live="polite">Abrindo o Admin VIW…</main>}
@else if(!api.session()){
 <main class="login"><section><a class="wordmark" href="/">VIW<span>ADMIN</span></a><p class="eyebrow">ÁREA RESTRITA</p><h1>Cuide da nossa<br>história.</h1><p>Entre com sua conta administrativa.</p>
 <form (ngSubmit)="login()" #form="ngForm"><label>E-mail<input class="form-control" type="email" name="email" [(ngModel)]="email" required autocomplete="username" maxlength="256"></label><label>Senha<input class="form-control" type="password" name="password" [(ngModel)]="password" required autocomplete="current-password" maxlength="512"></label><p role="alert" class="error">{{error()}}</p><button [disabled]="busy()||form.invalid">{{busy()?'Entrando…':'Entrar no Admin →'}}</button></form><small>Contas são criadas por um administrador. Não há cadastro público.</small></section><aside><span>VIW</span><h2>O jogo passa.<br>A história fica.</h2></aside></main>
}@else{
 <div class="app"><aside class="sidebar"><a routerLink="/players" class="wordmark">VIW<span>ADMIN</span></a><p class="eyebrow">GESTÃO ESPORTIVA</p><nav aria-label="Administração">
 <a routerLink="/players" routerLinkActive="active">Jogadores</a><a routerLink="/identities" routerLinkActive="active">Unir jogadores</a><a routerLink="/awards" routerLinkActive="active">Prêmios e seleções</a><a routerLink="/coaches" routerLinkActive="active">Técnicos</a><a routerLink="/assignments" routerLinkActive="active">Equipes e técnicos</a><a routerLink="/audit" routerLinkActive="active">Histórico de alterações</a>
 @if(api.session()!.roles.includes('Administrator')){<a routerLink="/accounts" routerLinkActive="active">Contas e acessos</a>}
 <a routerLink="/password" routerLinkActive="active">Minha senha</a></nav><div class="sidebar-bottom"><p>{{api.session()!.email}}</p><button class="btn btn-link quiet" (click)="logout()" [disabled]="busy()">Sair</button><p class="error" role="alert">{{error()}}</p></div></aside><main class="workspace"><router-outlet /></main></div>
}`})
export class App implements OnInit {
 api=inject(Api);loading=signal(true);busy=signal(false);error=signal('');email='';password='';
 async ngOnInit(){await this.api.restore();this.loading.set(false);}
 async login(){this.busy.set(true);this.error.set('');try{await this.api.login(this.email,this.password);this.password='';}catch(e){this.error.set(message(e));this.password='';}finally{this.busy.set(false);}}
 async logout(){this.busy.set(true);try{await this.api.logout();}catch(e){this.error.set(message(e));}finally{this.busy.set(false);}}
}
