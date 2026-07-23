<div align="center">
  <img src="https://raw.githubusercontent.com/devicons/devicon/master/icons/dot-net/dot-net-original.svg" alt="Logo" width="80" height="80">
  <h1 align="center">Radar de Tendências 🎯</h1>

  <p align="center">
    Plataforma inteligente de <strong>Social Listening</strong> e análise de <strong>Hype</strong> para obras de entretenimento (Animes, Filmes e Séries).
    <br />
    <br />
    <a href="#-features">Ver Features</a>
    ·
    <a href="#-tecnologias">Ver Tecnologias</a>
    ·
    <a href="#-como-executar">Instruções de Instalação</a>
  </p>

  <p align="center">
    <img src="https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet" alt=".NET 10" />
    <img src="https://img.shields.io/badge/Angular-21-DD0031?style=for-the-badge&logo=angular&logoColor=white" alt="Angular 21" />
    <img src="https://img.shields.io/badge/Python-3.11-3776AB?style=for-the-badge&logo=python&logoColor=white" alt="Python 3.11" />
    <img src="https://img.shields.io/badge/SQL_Server-2022-CC2927?style=for-the-badge&logo=microsoft-sql-server&logoColor=white" alt="SQL Server 2022" />
    <img src="https://img.shields.io/badge/Docker-2496ED?style=for-the-badge&logo=docker&logoColor=white" alt="Docker" />
    <img src="https://img.shields.io/badge/AniList-GraphQL-02A9FF?style=for-the-badge&logo=graphql&logoColor=white" alt="AniList GraphQL" />
  </p>
</div>

<details>
  <summary>Tabela de Conteúdos</summary>
  <ol>
    <li><a href="#-sobre-o-projeto">Sobre o Projeto</a></li>
    <li><a href="#-arquitetura">Arquitetura</a></li>
    <li><a href="#-features">Features Principais</a></li>
    <li><a href="#-tecnologias">Tecnologias</a></li>
    <li><a href="#-endpoints-da-api">Endpoints da API</a></li>
    <li><a href="#-como-executar">Como Executar</a></li>
  </ol>
</details>

## 📖 Sobre o Projeto

O **Radar de Tendências** é um ecossistema projetado para cruzar dados estáticos de catálogos de entretenimento com o engajamento orgânico de comunidades digitais.

Através da integração com múltiplas fontes (Reddit, YouTube, Jikan/MAL, TMDB e AniList GraphQL), o sistema processa textos de reviews, comentários, threads de fórum e sinopses, submetendo-os a um **Motor de Inteligência Artificial Local (NLP)** rodando um modelo transformer da Hugging Face (`cardiffnlp/twitter-xlm-roberta-base-sentiment`). Com base nesses dados, calcula um **Hype Score**, um **Score de Sentimento** e gera um **Resumo Executivo (Slang-Aware)** contextualizado para a cultura de fandom.

## 🏗 Arquitetura

O sistema adota uma abordagem de microsserviços rodando em containers Docker:

- **API Core (.NET 10):** Minimal API de altíssima performance reestruturada sob os preceitos de **Vertical Slice Architecture** e **CQRS (MediatR)**. Injeções limpas via métodos de extensão. Adotou-se o Dapper para acesso otimizado aos dados e JWT para Autenticação/Multi-Tenancy.
- **Worker Service (.NET 10):** Job em background em **CQRS** responsável por sincronizar avaliações, acionar pipelines de ML e monitorar o Hype.
- **NLP Service (Python):** Microsserviço de Processamento de Linguagem Natural com FastAPI (`cardiffnlp/twitter-xlm-roberta-base-sentiment`), varrendo sentimentos multilinguais.
- **LLM Service (Python):** Microsserviço recém-adicionado de IA Generativa Local rodando o modelo **Qwen 2.5 (1.5B)**. Encarregado de analisar entidades (Nuvem de Palavras) e produzir insights executivos avançados sobre cada franquia.
- **Frontend App (Angular 21):** Aplicação fluida usando **Feature-Based Architecture**. Consome a API, adota 100% de reatividade moderna (Signals, `toSignal`, RxJS Interop) prevenindo memory leaks e renderiza componentes standalone estilizados de modo escalável e validado globalmente por um ecosistema de ESLint e Prettier.

## ✨ Features Principais

| Módulo | Descrição |
|---|---|
| 🔍 **Busca Universal** | Unifica resultados de Animes (Jikan/MAL), Mangás, Filmes e Séries (TMDB), segregados por fonte com fallback AniList GraphQL |
| 🧠 **Análise de Sentimento (IA)** | Pipeline NLP local (Hugging Face) varrendo Reddit, MAL, TMDB, YouTube e Threads AniList |
| 🤖 **Resumo Executivo Generativo** | Motor local de IA (Qwen 2.5 1.5B) que consolida a Nuvem de Palavras, o Hype e o Sentimento em um insight executivo natural |
| 📈 **Hype Score Histórico** | Gráfico temporal de popularidade por franquia com variação de engajamento |
| 📅 **Análise Sazonal** | Dashboard de temporadas identificando Blockbusters vs. Joias Ocultas via AniList |
| 🗓 **Calendário Semanal** | Grade de lançamentos semanais com episódios e estúdios via AniList `airingSchedules` |
| 🌐 **Global vs. Brasil** | Comparativo de popularidade global (AniList) vs. engajamento local (YouTube/Reddit) |
| 🎭 **Linha do Tempo de Relações** | Prequelas, sequências e spin-offs mapeados via AniList GraphQL |
| 👥 **Personagens & Elenco** | Personagens populares (Jikan) e elenco de atores (TMDB) na tela de detalhes |
| 📌 **Watchlist & Favoritos** | Lista pessoal persistida no SQL Server com toggle via API |
| 🔔 **Central de Notificações** | Alertas acionados pelo Motor de Regras (Flow Editor), com CRUD completo e badge em tempo real |
| ⚙️ **Motor de Regras (Flow)** | Interface arrastar-e-soltar para criar automações (Ex: Se Hype > 80, dispare notificação) |
| 🏭 **Streaming Providers & Estúdios** | Tabelas `StreamingProviders` e `Estudios` para persistir dados de onde assistir |
| 🛡️ **NLP Resiliente** | Fallback automático de modelo (`distilbert`) caso `sentencepiece`/XLM-RoBERTa falhe no boot |
| ☁️ **Nuvem de Palavras** | Extração IA de entidades e tópicos chaves das comunidades, colorizados por sentimento |
| 📊 **Telemetria (Health Check)** | Painel de monitoramento do Worker, Redis, Memória API e SQL Server |
| 🚨 **Triggers e Alertas** | Motor de customização de alertas proativos com disparos condicionais (Hype > X) |
| 📑 **Relatórios em Excel** | Exportação analítica estruturada de mercado processada server-side via ClosedXML |
| 🛡️ **Segurança e JWT** | Endpoints resguardados, guards robustos no Angular e sistema de Autenticação/Multi-Tenancy robusto. |
| 📐 **Padrões de Engenharia** | Kebab-case root folders, Husky pré-commits (lint-staged), .editorconfig C# nativo e ESLint Flat. |

## 🛠 Tecnologias

| Camada | Tecnologias |
|---|---|
| **Backend** | C# 13, .NET 10 Minimal APIs, **MediatR (CQRS)**, Dapper, DbUp, SQL Server 2022, ClosedXML |
| **Worker** | .NET 10 Background Service, **MediatR**, IHttpClientFactory, PeriodicTimer |
| **Frontend** | Angular 21, SCSS, PrimeNG Icons, Chart.js, Signals, RxJS Interop, ESLint Flat Config, Prettier |
| **IA / NLP / LLM** | Python 3.11, FastAPI, Transformers, Torch, SentencePiece, Hugging Face (Qwen 2.5, XLM-RoBERTa) |
| **Infraestrutura** | Docker, Docker Compose, Nginx, Redis |
| **Integrações** | Jikan (MAL), AniList GraphQL, TMDB, Reddit JSON API, YouTube Data API v3 |

## 📡 Endpoints da API

| Método | Rota | Descrição |
|---|---|---|
| `GET` | `/franquias` | Lista todas as franquias |
| `GET` | `/monitoramento/dashboard` | Dashboard com Hype Score por franquia |
| `GET` | `/franquias/{id}/detalhes` | Detalhes + histórico + ResumoIA |
| `GET` | `/franquias/{id}/personagens` | Personagens (Jikan/TMDB) |
| `GET` | `/franquias/{id}/comunidade` | Reviews e posts de comunidade |
| `GET` | `/franquias/{id}/relacoes` | Relações (prequelas/sequências) via AniList |
| `GET` | `/franquias/{id}/comparativo-regional` | Global vs. Brasil (AniList + redes sociais) |
| `GET` | `/franquias/{id}/streaming` | Provedores de streaming da franquia |
| `GET` | `/franquias/{id}/estudios` | Estúdios responsáveis pela franquia |
| `GET` | `/franquias/{id}/palavras-chave` | Obter array JSON da nuvem de palavras |
| `GET` | `/temporadas/analise` | Animes por temporada (AniList GraphQL) |
| `GET` | `/calendario/semana` | Grade de lançamentos semanal (AniList) |
| `GET` | `/pesquisa?q={termo}` | Busca multi-fonte com fallback AniList |
| `POST` | `/franquias/sync` | Sincroniza franquia (upsert) |
| `POST` | `/monitoramento` | Registra novo ciclo de monitoramento |
| `GET` | `/favoritos/{userId}` | Lista watchlist do usuário |
| `POST` | `/favoritos/toggle/{franquiaId}` | Adiciona/remove da watchlist |
| `GET` | `/notificacoes` | Lista todas as notificações |
| `PATCH` | `/notificacoes/{id}/ler` | Marca notificação como lida |
| `DELETE` | `/notificacoes/{id}` | Remove notificação |
| `GET` | `/fluxos` | Lista motor de regras |
| `POST` | `/fluxos` | Cria/atualiza fluxo de regras |
| `GET` | `/telemetria/status` | Monitoramento e Health Check do ecosistema |
| `GET` | `/alertas` | Lista alertas de usuário configurados |
| `POST` | `/alertas` | Cria novo alerta condicional |
| `DELETE` | `/alertas/{id}` | Apaga um alerta ativo |
| `GET` | `/relatorios/mercado/excel` | Retorna o relátório Excel (`blob` / `.xlsx`) |

## 🚀 Como Executar

### Pré-requisitos
* [Docker](https://docs.docker.com/get-docker/) e [Docker Compose](https://docs.docker.com/compose/install/)
* Git

### 1. Clonar o Repositório
```bash
git clone https://github.com/ErickG123/radar-tendencias.git
cd radar-tendencias
```

### 2. Configurar as Chaves de API
**`worker/appsettings.json`** e **`api/appsettings.json`**:
```json
{
  "TmdbApiKey": "SUA_CHAVE_TMDB",
  "YouTubeApiKey": "SUA_CHAVE_YOUTUBE"
}
```

### 3. Iniciar a Aplicação
```bash
docker-compose up -d --build
```
> 💡 **Nota:** A primeira execução pode levar alguns minutos devido ao download dos modelos NLP da Hugging Face.

### 4. Acessos aos Serviços

| Serviço | URL | Descrição |
|---|---|---|
| **Frontend App** | `http://localhost:4200` | Interface visual do usuário |
| **API (.NET)** | `http://localhost:8080` | Core API |
| **NLP (Python)** | `http://localhost:5000` | Microsserviço NLP Analítico |
| **LLM (Python)** | `http://localhost:5001` | Microsserviço de IA Generativa (Qwen 2.5) |
| **Banco de Dados** | `localhost,14333` | SQL Server (`sa` / `Radar@Db2026!`) |

---

<p align="center">
Feito com dedicação técnica para análise inteligente de dados. 🚀
</p>
