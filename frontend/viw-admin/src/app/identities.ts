import {Component,inject,signal,OnInit} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {Api,Edition,Source,Person,Choice,Page,message} from './api';
import {PlayerPhoto} from './player-photo';
@Component({selector:'viw-identities',standalone:true,imports:[FormsModule,PlayerPhoto],template:`
<header class="page-header"><div><p class="eyebrow">VIW / IDENTIDADES</p><h1>Unir jogadores</h1></div></header>
<p class="error" role="alert">{{error()}}</p><p class="success" role="status">{{notice()}}</p>
@if(!active()){
<p>Escolha um cadastro para começar. A pessoa só aparecerá na Wiki após você confirmar. Um único cadastro também pode criar uma pessoa.</p>
<div class="toolbar"><label>Copa<select [(ngModel)]="cup" (ngModelChange)="cupChanged()"><option value="">Todas</option>@for(e of editions();track e.id){<option [value]="e.id">{{label(e.id)}}</option>}</select></label><label>Equipe<select [(ngModel)]="team" (ngModelChange)="load()" [disabled]="!cup||busy()"><option value="">Todas as equipes</option>@for(t of teams();track t.id){<option [value]="t.id">{{t.name}}</option>}</select></label><label>Cadastro na Wiki<select [(ngModel)]="unlinkedOnly" (ngModelChange)="load()"><option [ngValue]="false">Todos os jogadores</option><option [ngValue]="true">Ainda sem Player</option></select></label><label>Nome<input [(ngModel)]="search" (keyup.enter)="load()" maxlength="100"></label><button (click)="load()" [disabled]="busy()">Buscar</button></div>
<div class="panel table-responsive"><table class="table align-middle"><thead><tr><th>Cadastro da copa</th><th>Copa</th><th>Wiki</th><th></th></tr></thead><tbody>@for(s of rows();track s.id){<tr><td><viw-player-photo [sourceId]="s.id" [name]="s.name"/> {{s.name}}<small class="source-team">{{teamNames(s)}}</small></td><td>{{label(s.editionId)}}</td><td>{{s.playerName||'Ainda não confirmado'}}</td><td><button [disabled]="busy()" (click)="start(s)">{{s.playerId?'Editar pessoa':'Começar união'}}</button></td></tr>}@empty{<tr><td colspan="4">Nenhum cadastro encontrado.</td></tr>}</tbody></table></div>
<div class="actions"><button [disabled]="page===1||busy()" (click)="page=page-1;load(false)">Anterior</button><span>Página {{page}} · {{total}} cadastros</span><button [disabled]="page*30>=total||busy()" (click)="page=page+1;load(false)">Próxima</button></div>
}@else{
<section class="panel editor"><h2>{{editing?'Editar pessoa':'Criar pessoa na Wiki'}}</h2>
@if(basePlayer();as base){
<div class="review comparison-base" role="region" aria-label="Jogador base de comparação">
<viw-player-photo [sourceId]="base.id" [name]="base.name" [enlarged]="true"/>
<div><h3>Jogador base de comparação</h3><strong>{{base.name}}</strong><p>{{label(base.editionId)}}</p><p class="source-team">{{teamNames(base)}}</p></div>
</div>
}
<p>Selecione no máximo um cadastro em cada copa. Um cadastro que já pertence a outra pessoa fica indisponível.</p>
@if(step<steps.length){
<h3>Copa {{step+1}} de {{steps.length}} · {{label(currentCup())}}</h3>
<form class="toolbar" (ngSubmit)="candidates()"><label>Equipe nesta copa<select name="candidateTeam" [(ngModel)]="candidateTeam" (ngModelChange)="candidates()" [disabled]="busy()"><option value="">Todas as equipes</option>@for(t of candidateTeams();track t.id){<option [value]="t.id">{{t.name}}</option>}</select></label><label>Buscar nesta copa<input name="candidateSearch" [(ngModel)]="candidateSearch" maxlength="100" placeholder="Nome, sobrenome ou inicial: Edvaldo Farias, E. Santana"></label><button [disabled]="busy()">Buscar</button></form>
<p class="hint">A busca ignora acentos e considera partes do nome, iniciais e pequenas diferenças de escrita. Nomes exatos aparecem primeiro. Compare a foto e as equipes antes de unir; nomes parecidos não confirmam a mesma pessoa.</p>
<p>Selecionado: <strong>{{selected().find(byCurrent)?.name||'Nenhum jogador desta copa'}}</strong></p>
<div class="choices">@for(s of choices();track s.id){<button [class.selected]="selected().some(isSelected(s.id))" [disabled]="busy()||!!s.playerId&&s.playerId!==editing" (click)="choose(s)"><viw-player-photo [sourceId]="s.id" [name]="s.name" [enlarged]="true"/> <strong>{{s.name}}</strong><small class="source-team">{{teamNames(s)}}</small> @if(s.playerId&&s.playerId!==editing){<small>Já pertence a {{s.playerName}}</small>}</button>}</div>
<p>{{choiceTotal}} resultado(s) · página {{choicePage}}</p><div class="actions"><button [disabled]="choicePage===1||busy()" (click)="choicePage=choicePage-1;candidates(false)">← Resultados anteriores</button><button [disabled]="choicePage*30>=choiceTotal||busy()" (click)="choicePage=choicePage+1;candidates(false)">Próximos resultados →</button></div>
<div class="actions"><button [disabled]="busy()" (click)="none()">Nenhum desta copa</button><button [disabled]="step===0||busy()" (click)="move(-1)">Voltar</button><button [disabled]="busy()" (click)="move(1)">Próxima copa →</button><button [disabled]="busy()" (click)="step=steps.length">Revisar seleção</button></div>
}@else{
<h3>Revisar e confirmar</h3><p>Os nomes abaixo continuarão disponíveis na busca da Wiki. Escolha o nome final para o perfil.</p>
@for(s of selected();track s.id){<div class="review"><viw-player-photo [sourceId]="s.id" [name]="s.name" [enlarged]="true"/> <span>{{s.name}} · {{label(s.editionId)}}<small class="source-team">{{teamNames(s)}}</small></span> <button [disabled]="busy()" (click)="remove(s.id)">Remover</button></div>}
<label>Nome final do jogador<input [(ngModel)]="name" maxlength="100" list="originalNames"></label><datalist id="originalNames">@for(s of selected();track s.id){<option [value]="s.name"></option>}</datalist>
<div class="actions"><button [disabled]="busy()" (click)="step=0;loadCandidateTeams()">Revisar copas</button><button [disabled]="busy()||!selected().length||name.trim().length<2" (click)="save()">{{editing?'Salvar alterações':'Criar pessoa e publicar na Wiki'}}</button></div>
}
<div class="actions"><button [disabled]="busy()" (click)="cancel()">Cancelar</button>@if(editing){<button [disabled]="busy()" (click)="deleteReview=true">Excluir pessoa da Wiki</button>}</div>
@if(deleteReview){<div class="review"><p>Excluir esta identidade e liberar seus cadastros? As fotos, prêmios e estatísticas das copas serão preservados.</p><button [disabled]="busy()" (click)="erase()">Confirmar exclusão</button><button (click)="deleteReview=false">Manter pessoa</button></div>}
</section>}
`,styles:[`
.comparison-base{margin:16px 0 24px;padding:16px;background:#e8f0f6;border:1px solid #cfe9f8;border-radius:6px}.comparison-base>div{flex:1 1 180px;min-width:0;overflow-wrap:anywhere}.comparison-base h3{margin:0 0 8px}.comparison-base p{margin:4px 0 0}
:host{display:block;min-width:0}.toolbar{flex-wrap:wrap;gap:16px}.toolbar label{flex:1 1 190px;min-width:0}.actions{flex-wrap:wrap;align-items:center;gap:12px;margin-top:20px}.editor{max-width:1100px}.editor>.actions{padding-top:16px;border-top:1px solid #e6edf2}.choices{display:grid;grid-template-columns:repeat(auto-fill,minmax(160px,1fr));gap:12px}.choices button{display:flex;flex-direction:column;align-items:center;justify-content:center;min-height:130px;gap:8px;color:#163c55;background:#e8f0f6;overflow-wrap:anywhere}.choices button.selected{background:#cfe9f8;color:#163c55}.table-responsive{overflow-x:auto}td:first-child{min-width:180px}td button{white-space:nowrap}h3{font-size:1.3rem;line-height:1.4}.review{display:flex;align-items:center;gap:12px;flex-wrap:wrap}@media(max-width:600px){.panel{padding:16px}.toolbar label{flex-basis:100%}.actions button{flex:1 1 140px}.choices{grid-template-columns:repeat(2,minmax(0,1fr))}}
`]})
export class Identities implements OnInit{
 basePlayer=signal<Source|null>(null);
 api=inject(Api);editions=signal<Edition[]>([]);rows=signal<Source[]>([]);choices=signal<Source[]>([]);selected=signal<Source[]>([]);active=signal(false);busy=signal(false);error=signal('');notice=signal('');steps:Edition[]=[];cup='';team='';unlinkedOnly=false;teams=signal<Choice[]>([]);candidateTeams=signal<Choice[]>([]);candidateTeam='';search='';page=1;total=0;step=0;candidateSearch='';choicePage=1;choiceTotal=0;editing:string|null=null;version='';name='';deleteReview=false;
 async ngOnInit(){try{this.editions.set(await this.api.all<Edition>('v1/championships'));await this.load();}catch(e){this.error.set(message(e));}}
 label(id:string){const e=this.editions().find(x=>x.id===id);return e?e.name+' · '+e.year+'/'+e.semester:id;}
 teamNames(source:Source){return source.teams?.length ? source.teams.map(team=>team.name).join(' · ') : 'Equipe não registrada';}
 currentCup(){return this.steps[this.step]?.id??'';}
 byCurrent=(s:Source)=>s.editionId===this.currentCup();
 isSelected(id:string){return (s:Source)=>s.id===id;}
 async cupChanged(){this.team='';this.teams.set([]);try{this.teams.set(await this.getTeams(this.cup));await this.load();}catch(e){this.error.set(message(e));}}
 async getTeams(id:string){const e=this.editions().find(x=>x.id===id);return e?.slug?await this.api.all<Choice>('v1/championships/'+e.slug+'/teams'):[];}
 async loadCandidateTeams(){this.candidateTeam='';this.candidateTeams.set([]);try{this.candidateTeams.set(await this.getTeams(this.currentCup()));await this.candidates();}catch(e){this.error.set(message(e));}}
 async load(reset=true){if(reset)this.page=1;this.busy.set(true);try{const p=await this.api.get<Page<Source>>('admin/sources?page='+this.page+'&search='+encodeURIComponent(this.search)+(this.cup?'&editionId='+this.cup:'')+(this.team?'&teamId='+this.team:'')+'&unlinkedOnly='+this.unlinkedOnly);this.rows.set(p.items);this.total=p.totalCount;}catch(e){this.error.set(message(e));}finally{this.busy.set(false);}}
 async start(s:Source){this.busy.set(true);this.error.set('');try{this.editing=s.playerId;this.name=s.name;this.version='';if(s.playerId){const x=await this.api.get<{player:Person;sources:Source[]}>('admin/identities/'+s.playerId);this.selected.set(x.sources);this.name=x.player.name;this.version=x.player.version;}else this.selected.set([s]);this.basePlayer.set(this.selected().find(x=>x.id===s.id)??s);this.steps=this.editing?this.editions():this.editions().filter(e=>e.id!==s.editionId);this.step=0;this.candidateSearch=s.name;this.active.set(true);}catch(e){this.error.set(message(e));}finally{this.busy.set(false);}if(this.active()&&this.steps.length)await this.loadCandidateTeams();}
 async candidates(reset=true){if(reset)this.choicePage=1;this.busy.set(true);this.choices.set([]);try{const p=await this.api.get<Page<Source>>('admin/sources?editionId='+this.currentCup()+'&search='+encodeURIComponent(this.candidateSearch)+'&page='+this.choicePage+(this.candidateTeam?'&teamId='+this.candidateTeam:''));this.choices.set(p.items);this.choiceTotal=p.totalCount;}catch(e){this.error.set(message(e));}finally{this.busy.set(false);}}
 choose(s:Source){this.selected.update(xs=>[...xs.filter(x=>x.editionId!==s.editionId),s]);}
 none(){this.selected.update(xs=>xs.filter(x=>x.editionId!==this.currentCup()));void this.move(1);}
 remove(id:string){this.selected.update(xs=>xs.filter(x=>x.id!==id));}
 async move(n:number){this.step+=n;if(this.step<this.steps.length)await this.loadCandidateTeams();}
 cancel(){this.active.set(false);this.basePlayer.set(null);this.deleteReview=false;this.error.set('');}
 async save(){this.busy.set(true);try{await this.api.change(this.editing?'PUT':'POST','admin/identities'+(this.editing?'/'+this.editing:''),{name:this.name,sourceIds:this.selected().map(x=>x.id),version:this.version||null});this.cancel();this.notice.set('Pessoa confirmada. As estatísticas de cada copa permanecem separadas.');await this.load();}catch(e){this.error.set(message(e));}finally{this.busy.set(false);}}
 async erase(){this.busy.set(true);try{await this.api.change('DELETE','admin/identities/'+this.editing+'?version='+encodeURIComponent(this.version),{});this.cancel();this.notice.set('Identidade excluída. Cadastros e estatísticas preservados.');await this.load();}catch(e){this.error.set(message(e));}finally{this.busy.set(false);}}
}
