SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[table1](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[count] [int] NOT NULL,
	[name] [nvarchar](50) NOT NULL,
 CONSTRAINT [PK_table1] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
SET IDENTITY_INSERT [dbo].[table1] ON 

GO
INSERT [dbo].[table1] ([id], [count], [name]) VALUES (1, 5, N'wheel')
GO
INSERT [dbo].[table1] ([id], [count], [name]) VALUES (2, 8, N'book')
GO
INSERT [dbo].[table1] ([id], [count], [name]) VALUES (3, 7, N'smartphone')
GO
INSERT [dbo].[table1] ([id], [count], [name]) VALUES (4, 1, N'lens')
GO
INSERT [dbo].[table1] ([id], [count], [name]) VALUES (5, 0, N'brain')
GO
SET IDENTITY_INSERT [dbo].[table1] OFF
GO