IF NOT EXISTS (SELECT 1 FROM Categorias WHERE Nome = 'Filmes e Séries')
BEGIN
    INSERT INTO Categorias (Nome) VALUES ('Filmes e Séries');
END
