
-- --------------------------------------------------
-- Date Created: 01/27/2013 02:38:18
-- compatible SQLite
-- Generated from EDMX file: C:\Users\Greg\documents\visual studio 2010\Projects\Howler\Howler\Core\Database\Collection.edmx
-- --------------------------------------------------
-- Dropping existing tables
-- --------------------------------------------------

    
	DROP TABLE if exists [Tracks];
    
	DROP TABLE if exists [Artists];
    
	DROP TABLE if exists [Albums];
    
	DROP TABLE if exists [Genres];
    
	DROP TABLE if exists [TrackArtist];
    
	DROP TABLE if exists [AlbumArtist];
    
	DROP TABLE if exists [TrackGenre];

-- --------------------------------------------------
-- Creating all tables
-- --------------------------------------------------

-- Creating table 'Tracks'
CREATE TABLE [Tracks] (
    [Path] nvarchar(2147483647)   NOT NULL ,
    [Title] nvarchar(2147483647)   NULL ,
    [Id] integer PRIMARY KEY AUTOINCREMENT  NOT NULL ,
    [TrackNumber] integer   NULL ,
    [Date] datetime   NULL ,
    [Rating] integer   NULL ,
    [DateAdded] datetime   NOT NULL ,
    [Bitrate] integer   NOT NULL ,
    [Playcount] integer   NOT NULL ,
    [DateLastPlayed] datetime   NULL ,
    [Size] integer   NOT NULL ,
    [Codec] nvarchar(2147483647)   NULL ,
    [BPM] integer   NULL ,
    [MusicBrainzId] nvarchar(2147483647)   NULL ,
    [Duration] integer   NOT NULL ,
    [ChannelCount] integer   NOT NULL ,
    [SampleRate] integer   NOT NULL ,
    [BitsPerSample] integer   NOT NULL ,
    [TagLibHash] nvarchar(2147483647)   NOT NULL ,
    [Album_Title] nvarchar(2147483647)   NULL ,
    [Album_ArtistsHash] nvarchar(2147483647)   NULL 
			
		,CONSTRAINT [FK_TrackAlbum]
    		FOREIGN KEY ([Album_Title], [Album_ArtistsHash])
    		REFERENCES [Albums] ([Title], [ArtistsHash])					
    		
			);

-- Creating table 'Artists'
CREATE TABLE [Artists] (
    [Name] nvarchar(2147483647)   NOT NULL ,
    [Id] integer PRIMARY KEY AUTOINCREMENT  NOT NULL ,
    [MusicBrainzId] nvarchar(2147483647)   NULL 
);

-- Creating table 'Albums'
CREATE TABLE [Albums] (
    [Title] nvarchar(2147483647)   NOT NULL ,
    [Disc] integer   NULL ,
    [TotalDiscs] integer   NULL ,
    [MusicBrainzId] nvarchar(2147483647)   NULL ,
    [ArtistsHash] nvarchar(2147483647)   NOT NULL 
 , PRIMARY KEY ([Title], [ArtistsHash])	
		);

-- Creating table 'Genres'
CREATE TABLE [Genres] (
    [Name] nvarchar(2147483647)   NOT NULL 
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
    [Album_Title] nvarchar(2147483647)   NOT NULL ,
    [Album_ArtistsHash] nvarchar(2147483647)   NOT NULL ,
    [Artists_Id] integer   NOT NULL 
 , PRIMARY KEY ([Album_Title], [Album_ArtistsHash], [Artists_Id])	
					
		,CONSTRAINT [FK_AlbumArtist_Album]
    		FOREIGN KEY ([Album_Title], [Album_ArtistsHash])
    		REFERENCES [Albums] ([Title], [ArtistsHash])					
    		
						
		,CONSTRAINT [FK_AlbumArtist_Artist]
    		FOREIGN KEY ([Artists_Id])
    		REFERENCES [Artists] ([Id])					
    		
			);

-- Creating table 'TrackGenre'
CREATE TABLE [TrackGenre] (
    [Track_Id] integer   NOT NULL ,
    [Genres_Name] nvarchar(2147483647)   NOT NULL 
 , PRIMARY KEY ([Track_Id], [Genres_Name])	
					
		,CONSTRAINT [FK_TrackGenre_Track]
    		FOREIGN KEY ([Track_Id])
    		REFERENCES [Tracks] ([Id])					
    		
						
		,CONSTRAINT [FK_TrackGenre_Genre]
    		FOREIGN KEY ([Genres_Name])
    		REFERENCES [Genres] ([Name])					
    		
			);


-- --------------------------------------------------
-- Script has ended
-- --------------------------------------------------