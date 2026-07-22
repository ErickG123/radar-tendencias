CREATE TABLE Categorias (
    CategoriaID INT IDENTITY(1,1) PRIMARY KEY,
    Nome VARCHAR(100) NOT NULL,
    DataCriacao DATETIME DEFAULT GETDATE()
);

CREATE TABLE Franquias (
    FranquiaID INT IDENTITY(1,1) PRIMARY KEY,
    Nome VARCHAR(150) NOT NULL,
    CategoriaID INT NOT NULL,
    Ativo BIT DEFAULT 1,
    DataCriacao DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_Franquias_Categorias FOREIGN KEY (CategoriaID) REFERENCES Categorias(CategoriaID)
);

CREATE TABLE MonitoramentoHype (
    MonitoramentoID BIGINT IDENTITY(1,1) PRIMARY KEY,
    FranquiaID INT NOT NULL,
    HypeScore DECIMAL(5,2) NOT NULL,
    VolumeMencoes INT NOT NULL,
    SentimentoPositivo DECIMAL(5,2),
    DataMedicao DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Monitoramento_Franquias FOREIGN KEY (FranquiaID) REFERENCES Franquias(FranquiaID)
);

CREATE NONCLUSTERED INDEX IX_MonitoramentoHype_DataMedicao 
ON MonitoramentoHype(DataMedicao);

CREATE NONCLUSTERED INDEX IX_MonitoramentoHype_Franquia_Data 
ON MonitoramentoHype(FranquiaID, DataMedicao)
INCLUDE (HypeScore, VolumeMencoes);
