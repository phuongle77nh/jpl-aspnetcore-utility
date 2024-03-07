IF NOT EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND OBJECT_ID = OBJECT_ID('[Grant].[GetPermissionByUserId]'))
   EXEC('CREATE PROCEDURE [Grant].[GetPermissionByUserId] AS BEGIN SET NOCOUNT ON; END')
GO
ALTER PROCEDURE [Grant].[GetPermissionByUserId]
(
	@ServiceName NVARCHAR(1000),
	@InputTableName NVARCHAR(1000),
	@InputTableSchema NVARCHAR(1000),
	@InputUserId NVARCHAR(200),
	@InputDebug BIT = 0
)
AS 
BEGIN
	DECLARE @debug bit = @InputDebug
	DECLARE @serviceId UNIQUEIDENTIFIER
	DECLARE @serviceEntityId UNIQUEIDENTIFIER
	DECLARE @databaseName NVARCHAR(256)
	DECLARE @tableSchema NVARCHAR(256)
	DECLARE @tableName NVARCHAR(256)
	DECLARE @userId NVARCHAR(200) = @InputUserId

	SELECT @databaseName = s.[Database], @tableSchema = se.TableSchema, @tableName = se.TableName FROM JplSecurity.[Grant].Services s
	JOIN JplSecurity.[Grant].ServiceEntities se on s.Id = se.ServiceId 
	WHERE s.ServiceName = @ServiceName AND se.TableName = @InputTableName AND se.TableSchema = @InputTableSchema
	
	IF OBJECT_ID('tempdb..#tempTable202308301415') IS NOT NULL DROP TABLE #tempTable202308301415
	CREATE TABLE #tempTable202308301415 
	(
		UserId UNIQUEIDENTIFIER,
		EntityId UNIQUEIDENTIFIER, 
		Permission INT
	)
	
	IF (@debug = 1) PRINT(@tableSchema)
	IF (@tableSchema <> '')
	BEGIN
		IF (@debug = 1) PRINT('Start get permission')
		
		DECLARE @generatedTableName NVARCHAR(1000) = 'JplSecurity.[Generated].' + LOWER(@databaseName) + '_' + LOWER(@tableSchema) + '_' + LOWER(@tableName) + ' '
		IF (@debug = 1) PRINT(@generatedTableName)
		
		DECLARE @sqlFinal NVARCHAR(MAX)= 'INSERT INTO #tempTable202308301415 SELECT ur.UserId, securedRole.EntityId, securedRole.Permission FROM ' + @generatedTableName + ' AS securedRole '
		+ 'JOIN JplAuthentication.[Identity].UserRoles ur ON ur.RoleId = securedRole.SourceId AND securedRole.SourceType = 1 '
		+ 'WHERE ur.UserId =''' + CONVERT(NVARCHAR(100),@userId) + ''''
		+ 'UNION SELECT securedUser.SourceId AS UserId, securedUser.EntityId, securedUser.Permission FROM ' + @generatedTableName + ' AS securedUser '
		+ 'WHERE securedUser.SourceId =''' + CONVERT(NVARCHAR(100),@userId) + ''''
		IF (@debug = 1) PRINT(@sqlFinal)
		
		EXEC sp_executesql @sqlFinal
	END
	
	SELECT * FROM #tempTable202308301415
END