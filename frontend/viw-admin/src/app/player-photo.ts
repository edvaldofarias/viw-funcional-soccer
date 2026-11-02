import { Component, Input } from '@angular/core';

@Component({selector:'viw-player-photo',standalone:true,template:`
<span class="admin-photo" aria-hidden="true"><span>{{name.slice(0,1)}}</span>
@if(sourceId||playerId){<img [src]="'/api/media/'+(sourceId?'sourceplayers/':'players/')+(sourceId||playerId)+(editionId?'?editionId='+editionId:'')"
 alt="" loading="lazy" (error)="failed($event)" (load)="loaded($event)">}
</span>`})
export class PlayerPhoto {
 @Input() sourceId:string|null=null;
 @Input() playerId:string|null=null;
 @Input() editionId:string|null=null;
 @Input() name='';
 failed(event:Event){(event.target as HTMLImageElement).hidden=true;}
 loaded(event:Event){(event.target as HTMLImageElement).hidden=false;}
}
