Por favor, registre o subagente  dotnet-boilerplate-architect  e invoque-o para criar a estrutura do meu novo projeto.
  
  Orientações para o subagente:
   1. Leia as regras de codificação em  .gemini/rules/  em ordem numérica para entender nossos padrões de código (Allman braces, file-scoped namespaces, padrão Result, validações, etc.).
   2. Leia as instruções em  .gemini/skills/  de 01 a 07 em ordem sequencial.
   3. Antes de começar a gerar os arquivos, me pergunte:
       • O nome da solução/projeto (ex: "StoreBackend").
       • Qual tecnologia de banco de dados relacional vamos usar (PostgreSQL, SQL Server, MySQL, SQLite ou Oracle) para que você instale os pacotes NuGet corretos.
       • A Connection String de comunicação com esse banco de dados.
   4. Execute a criação das camadas do projeto (Clean Architecture), o mapeamento do banco de dados com  DatabaseOptions , a injeção de dependência modular, a criação dos
   Controllers e os testes de unidade com Moq conforme instruído nas skills.
   5. Ao final, execute  dotnet build  para garantir que o projeto compila sem erros.