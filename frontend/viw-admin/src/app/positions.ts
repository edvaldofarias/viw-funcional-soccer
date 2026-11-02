import {Component,inject,signal,OnInit} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {Api,Edition,Source,Choice,Page,message} from './api';
import {PlayerPhoto} from './player-photo';
@Component({selector:'viw-positions',standalone:true,imports:[FormsModule,PlayerPhoto],template:`
<header class="page-header"><div><p class="eyebrow">VIW / CADASTROS DAS COPAS</p><h1>Posições e goleiros</h1></div></header>
<p>Defina a posição nesta copa. Sem seleção, o jogador é de linha. Marque Goleiro para identificar quem defendeu a equipe; isso não altera outras copas.</p>
<p role="alert" class="error">{{error()}}</p><p role="status" class="success">{{notice()}}</p>
<div class="toolbar"><label>Copa<select [(ngModel)]="cup" (ngModelChange)="cupChanged()" [disabled]="busy()"><option value="">Selecione</option>@for(e of editions();track e.id){<option [value]="e.id">{{e.name}} · {{e.year}}/{{e.semester}}</option>}</select></label>
<label>Equipe<select [(ngModel)]="team" (ngModelChange)="load()" [disabled]="busy()"><option value="">Todas as equipes</option>@for(t of teams();track t.id){<option [value]="t.id">{{t.name}}</option>}</select></label>
<label>Nome<input [(ngModel)]="search" (keyup.enter)="load()" maxlength="100"></label><label><input type="checkbox" [(ngModel)]="goalkeepers" (ngModelChange)="load()"> Só goleiros</label><button (click)="load()" [disabled]="!cup||busy()">Buscar</button></div>
<div class="panel table-responsive"><table class="table align-middle"><thead><tr><th>Jogador</th><th>Posição nesta copa</th><th></th></tr></thead><tbody>@for(s of rows();track s.id){<tr><td><viw-player-photo [sourceId]="s.id" [name]="s.name"/> {{s.name}}</td><td><select [(ngModel)]="s.position" [disabled]="busy()"><option [ngValue]="null">Jogador de linha (não especificada)</option>@for(p of positions;track p){<option [value]="p">{{p}}</option>}</select></td><td><button [disabled]="busy()" (click)="save(s)">Salvar posição</button></td></tr>}@empty{<tr><td colspan="3">{{cup?'Nenhum jogador encontrado.':'Selecione uma copa.'}}</td></tr>}</tbody></table></div>
<div class="actions"><button [disabled]="page===1||busy()" (click)="page=page-1;load(false)">Anterior</button><span>Página {{page}} · {{total}} jogadores</span><button [disabled]="page*30>=total||busy()" (click)="page=page+1;load(false)">Próxima</button></div>
`})
export class Positions implements OnInit{
 api=inject(Api);editions=signal<Edition[]>([]);teams=signal<Choice[]>([]);rows=signal<Source[]>([]);busy=signal(false);error=signal('');notice=signal('');cup='';team='';search='';goalkeepers=false;page=1;total=0;positions=['Goleiro','Zagueiro','Meio-campo','Ala','Atacante'];
 async ngOnInit(){try{this.editions.set(await this.api.all<Edition>('v1/championships'));}catch(e){this.error.set(message(e));}}
 async cupChanged(){this.team='';this.teams.set([]);const e=this.editions().find(x=>x.id===this.cup);try{if(e)this.teams.set(await this.api.all<Choice>('v1/championships/'+e.slug+'/teams'));await this.load();}catch(e){this.error.set(message(e));}}
 async load(reset=true){if(reset)this.page=1;if(!this.cup){this.rows.set([]);this.total=0;return;}this.busy.set(true);this.error.set('');try{const p=await this.api.get<Page<Source>>('admin/sources?editionId='+this.cup+'&page='+this.page+'&goalkeepers='+this.goalkeepers+'&search='+encodeURIComponent(this.search)+(this.team?'&teamId='+this.team:''));this.rows.set(p.items);this.total=p.totalCount;}catch(e){this.error.set(message(e));}finally{this.busy.set(false);}}
 async save(s:Source){this.busy.set(true);try{await this.api.change('PUT','admin/sources/'+s.id+'/position',{position:s.position,version:s.version});this.notice.set('Posição de '+s.name+' salva nesta copa.');await this.load(false);}catch(e){this.error.set(message(e));}finally{this.busy.set(false);}}
}
