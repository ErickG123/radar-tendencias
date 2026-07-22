export interface DashboardHype {
  FranquiaID: number;
  Nome: string;
  CategoriaID: number;
  HypeScore: number;
  VolumeMencoes: number;
  SentimentoPositivo: number;
  TagsString?: string;
  TagList?: string[];
}
