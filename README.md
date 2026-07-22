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
    <img src="https://img.shields.io/badge/Angular-DD0031?style=for-the-badge&logo=angular&logoColor=white" alt="Angular 21" />
    <img src="https://img.shields.io/badge/Python-3.11-3776AB?style=for-the-badge&logo=python&logoColor=white" alt="Python 3.11" />
    <img src="https://img.shields.io/badge/SQL_Server-2022-CC2927?style=for-the-badge&logo=microsoft-sql-server&logoColor=white" alt="SQL Server 2022" />
    <img src="https://img.shields.io/badge/Docker-2496ED?style=for-the-badge&logo=docker&logoColor=white" alt="Docker" />
  </p>
</div>

<details>
  <summary>Tabela de Conteúdos</summary>
  <ol>
    <li><a href="#-sobre-o-projeto">Sobre o Projeto</a></li>
    <li><a href="#-arquitetura">Arquitetura</a></li>
    <li><a href="#-features">Features Principais</a></li>
    <li><a href="#-tecnologias">Tecnologias</a></li>
    <li><a href="#-como-executar">Como Executar</a></li>
  </ol>
</details>

## 📖 Sobre o Projeto

O **Radar de Tendências** é um ecossistema projetado para cruzar dados estáticos de catálogos de entretenimento com o engajamento orgânico de comunidades digitais. 

Através da integração com múltiplas fontes (Reddit, YouTube, Jikan e TMDB), o sistema processa textos de reviews, comentários e sinopses e os submete a um **Motor de Inteligência Artificial Local (NLP)** rodando um modelo transformer da Hugging Face (`bert-base-multilingual-uncased-sentiment`). Com base nesses dados, o sistema calcula um **Hype Score** (termômetro de popularidade) e um **Score de Sentimento** (positivo/negativo/neutro).

## 🏗 Arquitetura

O sistema adota uma abordagem de microsserviços rodando em containers Docker:

- **API Core (.NET 10):** Minimal API de altíssima performance estruturada com `Dapper`. Responsável por servir dados ao Frontend, consultar bancos e realizar as buscas externas *In-Memory*.
- **Worker Service (.NET 10):** Job em *background* responsável por sincronizar as avaliações das comunidades externas e enviar requisições loteadas para o NLP Service, garantindo a atualização assíncrona do "Hype Score".
- **NLP Service (Python):** Microsserviço construído com `FastAPI` e a biblioteca `transformers`. Executa as análises de Sentimento multilinguais em tempo real utilizando poder computacional local, sem dependência de LLMs comerciais.
- **Frontend App (Angular):** Aplicação Single Page fluida que consome os dados e plota gráficos analíticos de forma rica, desenvolvida com `Standalone Components` e `Signals`.

## ✨ Features Principais

- 🔍 **Busca Universal Dinâmica:** Motor de busca que unifica resultados de Animes (Jikan/MyAnimeList), Mangás (Jikan) e Filmes/Séries (TMDB).
- 🧠 **Análise de Sentimento (IA):** Pipeline local rodando NLP (Hugging Face) para varrer críticas no Reddit, MyAnimeList, TMDB e YouTube.
- 📈 **Gráficos de Hype Histórico:** Painel visual na UI que rastreia a elevação (ou queda) de popularidade de uma obra através dos dias.
- 🎭 **Voz da Comunidade:** Uma timeline interativa (Feed) contendo resenhas, posts de redes sociais e reviews de YouTube sobre a franquia.
- ⚙️ **Configuração de Alertas (Node-Based):** Uma interface arrastar-e-soltar (Flow) para criar regras (Ex: Se Sentimento > 80, dispare Notificação).

## 🛠 Tecnologias

Abaixo as ferramentas essenciais que tornaram este projeto realidade:

* **Backend & Infraestrutura:** C# 13, .NET 10, SQL Server 2022, DbUp (Migrations), Docker Compose.
* **Frontend:** Angular 21, PrimeNG, SCSS Global, Chart.js.
* **Inteligência Artificial:** Python 3.11, FastAPI, Hugging Face, Uvicorn, Torch.
- **Integrações de API:** `api.jikan.moe` (Animes), `graphql.anilist.co` (Animes Fallback), `api.themoviedb.org` (Filmes/Séries), `reddit.com` (Fóruns), `googleapis.com/youtube/v3` (Vídeos).

## 🚀 Como Executar

### Pré-requisitos
Antes de começar, você precisará ter as seguintes ferramentas instaladas em sua máquina:
* [Docker](https://docs.docker.com/get-docker/) e [Docker Compose](https://docs.docker.com/compose/install/)
* Git (Para clonar o repositório)

### 1. Clonar o Repositório
```bash
git clone https://github.com/ErickG123/radar-tendencias.git
cd radar-tendencias
```

### 2. Configurar as Chaves de API
Você precisará de *API Keys* válidas para o TMDB e o YouTube.
Substitua os valores placeholders nos arquivos `appsettings.json` nos diretórios do **Worker** e da **API**:

**`RadarTendencias.Worker/appsettings.json`** e **`RadarTendencias.Api/appsettings.json`**:
```json
{
  "TmdbApiKey": "SUA_CHAVE_TMDB",
  "YouTubeApiKey": "SUA_CHAVE_YOUTUBE"
}
```

### 3. Iniciar a Aplicação (Docker)
Execute o comando na raiz do projeto (onde o arquivo `docker-compose.yml` reside):
```bash
docker-compose up -d --build
```
*💡 **Nota:** O Docker iniciará o download e compilação de toda a stack. A primeira execução pode levar alguns minutos devido aos downloads dos Modelos de NLP da Hugging Face no container Python.*

### 4. Acessos aos Serviços
Uma vez iniciados, os serviços estarão acessíveis através das seguintes URLs locais:

| Serviço            | URL                     | Descrição                                        |
| ------------------ | ----------------------- | ------------------------------------------------ |
| **Frontend App**   | `http://localhost:4200` | Interface visual final do usuário.               |
| **API (.NET)**     | `http://localhost:8080` | Core API (Consultas e Automações).               |
| **NLP (Python)**   | `http://localhost:5000` | Microsserviço de Inteligência Artificial.        |
| **Banco de Dados** | `localhost,14333`       | SQL Server (User: `sa` / Pass: `Radar@Db2026!`). |

---

<p align="center">
Feito com dedicação técnica para análise inteligente de dados. 🚀
</p>
