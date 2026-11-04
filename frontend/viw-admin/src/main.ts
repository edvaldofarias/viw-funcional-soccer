import { bootstrapApplication } from '@angular/platform-browser';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { App } from './app/app';
bootstrapApplication(App,{providers:[provideHttpClient(),provideRouter([
 {path:'',pathMatch:'full',redirectTo:'players'},
 {path:'identities',loadComponent:()=>import('./app/identities').then(m=>m.Identities)}, {path:'positions',loadComponent:()=>import('./app/positions').then(m=>m.Positions)},
 ...['players','awards','coaches','assignments','audit','accounts','password'].map(section=>({path:section,loadComponent:()=>import('./app/workspace').then(m=>m.Workspace),data:{section}})),
 {path:'**',redirectTo:'players'}
])]}).catch(()=>{document.body.textContent='Não foi possível abrir o Admin. Recarregue a página.';});
