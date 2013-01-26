
-- --------------------------------------------------
-- Date Created: 01/25/2013 19:12:30
-- compatible SQLite
-- Generated from EDMX file: C:\Users\Greg\documents\visual studio 2010\Projects\Howler\Howler\Core\Database\Collection.edmx
-- --------------------------------------------------
-- Dropping existing tables
-- --------------------------------------------------

    
	DROP TABLE if exists [Tracks];
    
	DROP TABLE if exists [Artists];
    
	DROP TABLE if exists [Albums];
    
	DROP TABLE if exists [TrackArtist];
    
	DROP TABLE if exists [AlbumArtist];

-- --------------------------------------------------
-- Creating all tables
-- --------------------------------------------------

-- Creating table 'Tracks'
CREATE TABLE [Tracks] (
    [Path] nvarchar(2147483647)   NOT NULL,
    [Title] nvarchar(2147483647)   NULL ,
    [Id] integer PRIMARY KEY AUTOINCREMENT  NOT NULL ,
    [Album_Id] integer   NULL 
			
		,CONSTRAINT [FK_TrackAlbum]
    		FOREIGN KEY ([Album_Id])
    		REFERENCES [Albums] ([Id])					
    		
			);

-- Creating table 'Artists'
CREATE TABLE [Artists] (
    [Name] nvarchar(2147483647)   NOT NULL,
    [Id] integer PRIMARY KEY AUTOINCREMENT  NOT NULL 
);

-- Creating table 'Albums'
CREATE TABLE [Albums] (
    [Id] integer PRIMARY KEY AUTOINCREMENT  NOT NULL ,
    [Title] nvarchar(2147483647)   NOT NULL 
);

-- Creating table 'TrackArtist'
CREATE TABLE [TrackArtist] (
    [Track_Id] integer   NOT NULL ,
    [Artists_Id] integer   NOT NULL 
 , PRIMARY KEY ([Track_Id], [Artists_Id])	
					
		,CONSTRAINT [FK_TrackArtist_Track]
    		FOREIGN KEY ([Track_Id])
    		REFERENCES [Tracks] ([Id])					
    		
						
		,CONSTRAINT [FK_TrackArtist_Artist]
    		FOREIGN KEY ([Artists_Id])
    		REFERENCES [Artists] ([Id])					
    		
			);

-- Creating table 'AlbumArtist'
CREATE TABLE [AlbumArtist] (
    [Album_Id] integer   NOT NULL ,
    [Artists_Id] integer   NOT NULL 
 , PRIMARY KEY ([Album_Id], [Artists_Id])	
					
		,CONSTRAINT [FK_AlbumArtist_Album]
    		FOREIGN KEY ([Album_Id])
    		REFERENCES [Albums] ([Id])					
    		
						
		,CONSTRAINT [FK_AlbumArtist_Artist]
    		FOREIGN KEY ([Artists_Id])
    		REFERENCES [Artists] ([Id])					
    		
			);


-- --------------------------------------------------
-- Script has ended
-- --------------------------------------------------