import { TestBed } from '@angular/core/testing';
import { PlayerPhoto } from './player-photo';

describe('Imagem de jogador', () => {
 afterEach(() => TestBed.resetTestingModule());

 function setup(playerId: string | null = null) {
  TestBed.configureTestingModule({ imports: [PlayerPhoto] });
  const fixture = TestBed.createComponent(PlayerPhoto);
  fixture.componentRef.setInput('playerId', playerId);
  fixture.componentRef.setInput('name', 'Nome reservado');
  fixture.detectChanges();
  return fixture;
 }

 it('mostra a imagem cinza externa quando não há identidade ou foto', () => {
  // Arrange
  const fixture = setup();
  // Act
  const image = fixture.nativeElement.querySelector('img') as HTMLImageElement;
  // Assert
  expect(image.getAttribute('src')).toBe(fixture.componentInstance.placeholder);
  expect(image.getAttribute('referrerpolicy')).toBe('no-referrer');
  expect(image.getAttribute('src')).not.toContain('Nome');
 });

 it('preserva a foto real e usa a alternativa quando ela falha', () => {
  // Arrange
  const fixture = setup('player-1');
  const image = fixture.nativeElement.querySelector('img') as HTMLImageElement;
  expect(image.getAttribute('src')).toBe('/api/media/players/player-1');
  // Act
  image.dispatchEvent(new Event('error'));
  fixture.detectChanges();
  // Assert
  expect(image.getAttribute('src')).toBe(fixture.componentInstance.placeholder);
  expect(image.hidden).toBe(false);
 });

 it('tenta a foto da nova identidade após a anterior falhar', () => {
  // Arrange
  const fixture = setup('player-1');
  (fixture.nativeElement.querySelector('img') as HTMLImageElement).dispatchEvent(new Event('error'));
  fixture.detectChanges();
  // Act
  fixture.componentRef.setInput('playerId', 'player-2');
  fixture.detectChanges();
  // Assert
  expect(fixture.nativeElement.querySelector('img').getAttribute('src')).toBe('/api/media/players/player-2');
 });

 it('não repete carregamentos indefinidamente se o placeholder também falhar', () => {
  // Arrange
  const fixture = setup('player-1');
  const image = fixture.nativeElement.querySelector('img') as HTMLImageElement;
  image.dispatchEvent(new Event('error'));
  fixture.detectChanges();
  // Act
  image.dispatchEvent(new Event('error'));
  fixture.detectChanges();
  // Assert
  expect(image.getAttribute('src')).toBe(fixture.componentInstance.placeholder);
  expect(fixture.nativeElement.querySelectorAll('img').length).toBe(1);
 });
});
