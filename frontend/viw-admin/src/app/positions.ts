import {Component,inject,signal,OnInit,OnDestroy} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {Api,Edition,Source,Choice,message} from './api';
import {PlayerPhoto} from './player-photo';
import {ViwDataTable} from './data-table';
@Component({selector:'viw-positions',standalone:true,imports:[FormsModule,PlayerPhoto,ViwDataTable],template:`
<header class="page-header"><div><p class="eyebrow">VIW / CADASTROS DAS COPAS</p><h1>Posições e goleiros</h1></div></header>
<p>Defina a posição nesta copa. Sem seleção, o jogador é de linha. Marque Goleiro para identificar quem defendeu a equipe; isso não altera outras copas.</p>
<p role="alert" class="error">{{error()}}</p><p role="status" class="success">{{notice()}}</p>
<div class="toolbar"><label>Copa<select [(ngModel)]="cup" (ngModelChange)="cupChanged()" [disabled]="busy()"><option value="">Selecione</option>@for(e of editions();track e.id){<option [value]="e.id">{{e.name}} · {{e.year}}/{{e.semester}}</option>}</select></label>
<label>Equipe<select [(ngModel)]="team" (ngModelChange)="load()" [disabled]="busy()"><option value="">Todas as equipes</option>@for(t of teams();track t.id){<option [value]="t.id">{{t.name}}</option>}</select></label>
<label>Nome<input [(ngModel)]="search" (keyup.enter)="load()" maxlength="100"></label><label><input type="checkbox" [(ngModel)]="goalkeepers" (ngModelChange)="load()"> Só goleiros</label><button (click)="load()" [disabled]="!cup||busy()">Buscar</button></div>
@if(!cup){<p>Selecione uma copa para listar os jogadores.</p>}
<div class="panel table-responsive"><table class="table align-middle" [viwDataTable]="rows()"><thead><tr><th>Jogador</th><th>Posição nesta copa</th><th data-dt-order="disable">Ações</th></tr></thead><tbody>@for(s of rows();track s.id){<tr><td [attr.data-order]="s.name" [attr.data-search]="s.name"><viw-player-photo [sourceId]="s.id" [name]="s.name"/> {{s.name}}</td><td [attr.data-order]="s.position||''" [attr.data-search]="s.position||'Jogador de linha'"><select [(ngModel)]="s.position" [disabled]="busy()"><option [ngValue]="null">Jogador de linha (não especificada)</option>@for(p of positions;track p){<option [value]="p">{{p}}</option>}</select></td><td><button [disabled]="busy()" (click)="save(s)">Salvar posição</button></td></tr>}</tbody></table></div>
`})
export class Positions implements OnInit,OnDestroy{
 private generation=0;
 api=inject(Api);editions=signal<Edition[]>([]);teams=signal<Choice[]>([]);rows=signal<Source[]>([]);busy=signal(false);error=signal('');notice=signal('');cup='';team='';search='';goalkeepers=false;positions=['Goleiro','Zagueiro','Meio-campo','Ala','Atacante'];
 ngOnDestroy(){this.generation++;}
 async ngOnInit(){try{this.editions.set(await this.api.all<Edition>('v1/championships'));}catch(e){this.error.set(message(e));}}
 async cupChanged(){this.team='';this.teams.set([]);const e=this.editions().find(x=>x.id===this.cup);try{if(e)this.teams.set(await this.api.all<Choice>('v1/championships/'+e.slug+'/teams'));await this.load();}catch(e){this.error.set(message(e));}}
 async load(){const generation=++this.generation;if(!this.cup){this.rows.set([]);this.busy.set(false);return;}this.busy.set(true);this.error.set('');try{const rows=await this.api.all<Source>('admin/sources?editionId='+this.cup+'&goalkeepers='+this.goalkeepers+'&search='+encodeURIComponent(this.search)+(this.team?'&teamId='+this.team:''));if(generation===this.generation)this.rows.set(rows);}catch(e){if(generation===this.generation)this.error.set(message(e));}finally{if(generation===this.generation)this.busy.set(false);}}
 async save(s:Source){this.busy.set(true);try{await this.api.change('PUT','admin/sources/'+s.id+'/position',{position:s.position,version:s.version});this.notice.set('Posição de '+s.name+' salva nesta copa.');await this.load();}catch(e){this.error.set(message(e));}finally{this.busy.set(false);}}
}
