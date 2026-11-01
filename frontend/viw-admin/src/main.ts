import { bootstrapApplication } from '@angular/platform-browser';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { App } from './app/app';
import { Workspace } from './app/workspace';
bootstrapApplication(App,{providers:[provideHttpClient(),provideRouter([
 {path:'',pathMatch:'full',redirectTo:'players'},
 ...['players','identities','awards','coaches','assignments','audit','accounts','password'].map(section=>({path:section,component:Workspace,data:{section}})),
 {path:'**',redirectTo:'players'}
])]}).catch(()=>{document.body.textContent='Não foi possível abrir o Admin. Recarregue a página.';});
