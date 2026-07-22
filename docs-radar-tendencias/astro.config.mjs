import { defineConfig } from 'astro/config';
import starlight from '@astrojs/starlight';

export default defineConfig({
  integrations: [
    starlight({
      title: 'Radar de Tendências Geek',
      sidebar: [
        {
          label: 'Visão Geral',
          link: '/visao-geral/',
        },
        {
          label: 'Architecture Decision Records',
          items: [
            { label: 'ADR 001 - Ingestão Orientada a Eventos', link: '/adrs/001-event-driven/' },
            { label: 'ADR 002 - Stack do Frontend', link: '/adrs/002-frontend-stack/' },
            { label: 'ADR 003 - Persistência Poliglota', link: '/adrs/003-polyglot-persistence/' },
            { label: 'ADR 004 - Governança e Hooks', link: '/adrs/004-governance/' },
            { label: 'ADR 005 - Migrations com DbUp no Docker', link: '/adrs/005-dbup-migrations/' },
            { label: 'ADR 006 - Coleta de Dados Reais e UI', link: '/adrs/006-jikan-api-ui/' },
            { label: 'ADR 007 - App Shell e Dashboard Dinâmico', link: '/adrs/007-app-shell-dashboard/' },
            { label: 'ADR 008 - Motor de Regras Visual (Canvas)', link: '/adrs/008-flow-editor/' },
            { label: 'ADR 009 - Análise de Sentimento (NLP)', link: '/adrs/009-nlp-microservice/' },
            { label: 'ADR 010 - Fallback AniList (GraphQL)', link: '/adrs/010-resiliencia-anilist/' },
          ],
        },
      ],
    }),
  ],
});
