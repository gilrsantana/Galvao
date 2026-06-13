import { routes } from './app.routes';

describe('App Routes', () => {
  it('should configure the correct title for home route', () => {
    const route = routes.find(r => r.path === '');
    expect(route?.title).toBe('Início | Galvão Design');
  });

  it('should configure the correct title for settings route', () => {
    const route = routes.find(r => r.path === 'settings');
    expect(route?.title).toBe('Configurações | Galvão');
  });

  it('should configure the correct title for showroom route', () => {
    const route = routes.find(r => r.path === 'showroom');
    expect(route?.title).toBe('Showroom de Móveis Minimalistas | Galvão');
  });

  it('should configure the correct title for showroom detail route', () => {
    const route = routes.find(r => r.path === 'showroom/:id');
    expect(route?.title).toBe('Detalhes do Produto | Galvão');
  });

  it('should configure the correct title for articles route', () => {
    const route = routes.find(r => r.path === 'articles');
    expect(route?.title).toBe('Artigos & Revista de Decoração | Galvão');
  });

  it('should configure the correct title for article detail route', () => {
    const route = routes.find(r => r.path === 'articles/:id');
    expect(route?.title).toBe('Detalhes do Artigo | Galvão');
  });

  it('should configure the correct title for login route', () => {
    const route = routes.find(r => r.path === 'login');
    expect(route?.title).toBe('Entrar | Galvão');
  });

  it('should configure the correct title for register route', () => {
    const route = routes.find(r => r.path === 'register');
    expect(route?.title).toBe('Criar Conta | Galvão');
  });

  it('should configure the correct title for admin route', () => {
    const route = routes.find(r => r.path === 'admin');
    expect(route?.title).toBe('Painel Administrativo | Galvão');
  });
});
