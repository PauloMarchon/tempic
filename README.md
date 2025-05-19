# 📄 Descrição do Projeto
A Image Expiration API é um serviço RESTful desenvolvido com ASP.NET Core que permite o upload de imagens, gera links únicos de acesso e gerencia a expiração automática dessas imagens. As imagens são armazenadas em um serviço de armazenamento de objetos (MinIO), e seus metadados (como link único e data de expiração) são persistidos em um banco de dados SQLite.

<br>

# ✨ Funcionalidades
- Upload de Imagens: Permite o envio de arquivos de imagem através de um endpoint HTTP POST.
- Geração de Links Únicos: Para cada imagem, um Guid único é gerado, formando um link acessível publicamente.
- Expiração Configurável: As imagens são armazenadas com uma data de expiração definida no momento do upload.
- Armazenamento de Objetos (MinIO): Utiliza MinIO como um serviço de armazenamento compatível com S3 para guardar os arquivos de imagem.
- Persistência de Metadados: Informações sobre as imagens (nome original, link único, localização no MinIO, datas de upload/expiração) são salvas em um banco de dados SQLite usando Entity Framework Core.
- Limpeza Automática em Segundo Plano: Um serviço em segundo plano verifica periodicamente e remove imagens e seus metadados do MinIO e do banco de dados quando elas expiram.

<br>

# 🛠️ Tecnologias Utilizadas
- Backend:
  - ASP.NET Core 8
  - MinIO .NET SDK
  - Entity Framework Core
  - SQLite
- Armazenamento de Objetos:
  - MinIO (via Docker)
    
<br>

# 🔌 API Endpoints
A aplicação expõe os seguintes endpoints RESTful:

📤 1. Upload de Imagem: 

Permite o upload de um arquivo de imagem com uma duração de expiração.

🖼️ 2. Obter Imagem: 

Permite acessar uma imagem através de seu link único.

<br>

# 📈 Evolução e Melhorias Futuras
Este projeto serve como um estudo de uma API de gerenciamento de imagens com expiração. Algumas melhorias serão implementadas ao longo do tempo:

- Otimização dos serviços de processamento de imagens:
    - Uploads por Blocos (Multipart Upload): Para arquivos extremamente grandes (vários GB), o MinIO suportam upload em blocos. Isso permite quebrar o arquivo em partes menores, que podem ser carregadas em paralelo e resumidas em caso de falha.
    - Serviço de Limpeza Automática: Verificar possível melhora no serviço de limpeza automática, que verifica periodicamente imagens expiradas para serem excluídas através de um Background Service.
- Resiliência e Observabilidade:
    - Circuit Breaker: Implementar um padrão de Circuit Breaker (ex: com Polly) para lidar com falhas temporárias na comunicação com o MinIO, evitando que a aplicação trave.
    - Métricas e Monitoramento: Adicionar métricas de desempenho (tempo de upload, taxa de erros, etc.) para monitoramento em tempo real (ex: Prometheus).
    - Rastreamento Distribuído: Para depurar em ambientes distribuídos, implementar o rastreamento (tracing) de requisições.
- Segurança:
    - Validação de Entrada Reforçada: Implementar validações mais rigorosas para tipos de arquivo (MIME types), tamanho máximo e conteúdo malicioso.
    - Limitação dos Tipos de Arquivos: Verificar e permitir apenas os tipos/extensões de arquivos referentes a imagens
- Gerenciamento de Erros:
    - Respostas de Erro Padronizadas: Retornar objetos de erro padronizados e mais detalhados para o cliente da API em caso de falhas.
    - Retries para Operações MinIO/DB: Adicionar lógica de retentativa para operações que podem falhar temporariamente (rede, banco de dados, MinIO).
- Escalabilidade e Alta Disponibilidade:
    - Configuração de Banco de Dados: Migrar de SQLite para um banco de dados mais robusto.
    - Cache: Implementar um cache (ex: Redis) para metadados de imagens frequentemente acessados, reduzindo a carga no banco de dados.
- Funcionalidades Adicionais:
    - UI para Gerenciamento: Desenvolver uma interface de usuário simples para visualizar e gerenciar as imagens.
- Refatoração:
    - Refatorar e implementar boas práticas e patterns onde cabível, promovendo uma melhor legibilidade, escalabilidade e desacoplamento do código.
- Testes:
    - Implementar testes robustos que visam cobrir pelo menos 70% do código da aplicação (priorizando testes em áreas mais sensíveis/importantes), evitando comportamentos indesejados e mantendo a aplicação previsível.

<br>

# 🤝 Contribuição
Contribuições são bem-vindas! Sinta-se à vontade para abrir issues, enviar pull requests ou sugerir melhorias.

<br>

# 🚀 Primeiros Passos 
Para rodar este projeto localmente:

Pré-requisitos
Certifique-se de ter o seguinte instalado em sua máquina:

- .NET 8 SDK
- Docker Desktop (para rodar o MinIO)

⬇️ 1. Clonar o Repositório
```
git clone https://github.com/YourUsername/YourRepoName.git
cd ImageExpirationApi
```

🗄️ 2. Configurar o MinIO (via Docker)
- Crie um volume Docker para persistir os dados do MinIO (opcional, mas recomendado)
- Inicie o MinIO usando Docker. Certifique-se de que as portas 9000 (API) e 9001 (Console UI) estão disponíveis.
- No console do MinIO, crie um bucket. O nome padrão usado nesta aplicação é image-bucket.
    
📊 3. Configurar o Banco de Dados (SQLite com EF Core)
- Navegue até o diretório raiz do projeto e aplique as migrações do Entity Framework Core para criar o banco de dados e o esquema:
```
dotnet ef database update
```
    
⚙️ 4. Configurar a Aplicação (appsettings.json)
- Edite o arquivo appsettings.json para corresponder às suas configurações do MinIO e do intervalo de limpeza.

🚀 5. Rodar a Aplicação
```
dotnet run
```
