IF NOT EXISTS (SELECT 1 FROM Categorias WHERE Nome = 'Animes')
BEGIN
    INSERT INTO Categorias (Nome) VALUES ('Animes');
END
