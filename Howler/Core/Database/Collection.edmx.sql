
-- --------------------------------------------------
-- Date Created: 01/25/2013 00:44:08
-- compatible SQLite
-- Generated from EDMX file: C:\Users\Greg\documents\visual studio 2010\Projects\Howler\Howler\Core\Database\Collection.edmx
-- --------------------------------------------------
-- Dropping existing tables
-- --------------------------------------------------

    
	DROP TABLE if exists [Tracks];

-- --------------------------------------------------
-- Creating all tables
-- --------------------------------------------------

-- Creating table 'Tracks'
CREATE TABLE [Tracks] (
    [Id] integer PRIMARY KEY AUTOINCREMENT  NOT NULL ,
    [Path] nvarchar(2147483647)   NOT NULL 
);


-- --------------------------------------------------
-- Script has ended
-- --------------------------------------------------