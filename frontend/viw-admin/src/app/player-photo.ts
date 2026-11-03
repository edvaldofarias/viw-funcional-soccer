import { Component, Input, OnChanges, signal } from '@angular/core';

@Component({selector:'viw-player-photo',standalone:true,template:`
<span class="admin-photo" aria-hidden="true"><span>{{name.slice(0,1)}}</span>
<img [src]="imageUrl" alt="" loading="lazy" referrerpolicy="no-referrer" (error)="failed()">
</span>`})
export class PlayerPhoto implements OnChanges {
 readonly placeholder = 'https://placehold.co/96x96/e5e7eb/e5e7eb?text=+';
 readonly imageFailed = signal(false);
 @Input() sourceId:string|null=null;
 @Input() playerId:string|null=null;
 @Input() editionId:string|null=null;
 @Input() name='';
 get imageUrl() {
  if(this.imageFailed() || !this.sourceId && !this.playerId) return this.placeholder;
  return '/api/media/'+(this.sourceId?'sourceplayers/':'players/')+(this.sourceId||this.playerId)+(this.editionId?'?editionId='+this.editionId:'');
 }
 ngOnChanges(){this.imageFailed.set(false);}
 failed(){this.imageFailed.set(true);}
}
