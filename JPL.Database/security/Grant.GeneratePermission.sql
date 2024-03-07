IF NOT EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND OBJECT_ID = OBJECT_ID('[Grant].[GeneratePermission]'))
   EXEC('CREATE PROCEDURE [Grant].[GeneratePermission] AS BEGIN SET NOCOUNT ON; END')
GO
ALTER PROCEDURE [Grant].[GeneratePermission]
(
	@InputServiceEntityId NVARCHAR(100),
	@InputDebug BIT = 0
)
AS 
BEGIN
	DECLARE @debug bit = @InputDebug
	DECLARE @serviceEntityId UNIQUEIDENTIFIER = @InputServiceEntityId
	DECLARE @databaseName NVARCHAR(256)
	DECLARE @tableSchema NVARCHAR(256)
	DECLARE @tableName NVARCHAR(256)
	DECLARE @tablePrimaryColumn NVARCHAR(256)
	DECLARE @myselfQuery NVARCHAR(256)
	
	SELECT  @databaseName = s.[Database], @tableSchema = se.TableSchema , @tableName = se.TableName, @tablePrimaryColumn = se.PrimaryColumnName, @myselfQuery = se.MyselfQuery FROM JplSecurity.[Grant].Services s JOIN JplSecurity.[Grant].ServiceEntities se ON s.Id = se.ServiceId WHERE se.Id = @serviceEntityId
	
	DECLARE @targetTableName NVARCHAR(1000) = @databaseName + '.[' + @tableSchema + '].[' + @tableName + '] '
	IF (@debug = 1) PRINT(@targetTableName);
	
	DECLARE @generatedTableName NVARCHAR(1000) = '[Generated].' + LOWER(@databaseName) + '_' + LOWER(@tableSchema) + '_' + LOWER(@tableName)
	IF (@debug = 1) PRINT('generatedTableName: ' + @generatedTableName);
	
	DECLARE @sqlCheckTableExist NVARCHAR(MAX) = 'IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'''+ @generatedTableName +''') AND type in (N''U'')) '
	DECLARE @generatedTableProperty NVARCHAR(MAX) = '(SourceId UNIQUEIDENTIFIER, EntityId UNIQUEIDENTIFIER, SourceType int, Permission int) '
	DECLARE @sqlCreateTable NVARCHAR(MAX) = @sqlCheckTableExist 
	+ 'BEGIN '
	+ 'CREATE TABLE ' + @generatedTableName 
	+ @generatedTableProperty
	+ 'END'
	
	DECLARE @generatedTableBackupName NVARCHAR(100) =  '#tempTableGeneratedPermSecured'
	IF (@debug = 1) PRINT ('generatedTableBackupName: ' + @generatedTableBackupName);
	

	-- SourceType - 1 Role, 2 User
	--IF OBJECT_ID('tempdb..#tempTableGeneratedPermSecured') IS NOT NULL DROP TABLE #tempTableGeneratedPermSecured
	DECLARE @sqlCreateTempTable NVARCHAR(MAX) = 'CREATE TABLE '+@generatedTableBackupName+' ( SourceId UNIQUEIDENTIFIER, EntityId UNIQUEIDENTIFIER, SourceType INT ,Permission INT)'
	IF (@debug = 1) PRINT ('sqlCreateTempTable');

	-- Generate permission for scope (ex: Leader/manager of department/group)
	DECLARE @sqlAttributeQuery AS NVARCHAR(MAX) = ''
	
	IF (@debug = 1) PRINT ('scope start')
	;WITH attributeQuery AS (SELECT a.AttributeQuery 'Query', p.Permission 
	FROM JplSecurity.[Grant].PolicyLink pl
	JOIN JplSecurity.[Grant].Policy p ON p.Id = pl.PolicyId AND pl.EntityType = 2
	JOIN JplSecurity.[Grant].[Attributes] a ON a.Id = pl.EntityId
	WHERE p.ServiceEntityId = @serviceEntityId)	
	SELECT @sqlAttributeQuery = STRING_AGG('INSERT INTO '+@generatedTableBackupName+' SELECT allAttrQuery.UserId AS SourceId, allAttrQuery.EntityId, attrPermission.SourceType, attrPermission.Permission FROM ((' + attrQuery.Query + ') AS allAttrQuery OUTER APPLY (SELECT '+ CONVERT(VARCHAR(10),attrQuery.Permission) +' AS ''Permission'', 2 AS ''SourceType'') AS attrPermission)',';') FROM attributeQuery attrQuery;
	
	IF (@debug = 1) PRINT ('scope end')
		
	-- Generate permission for role by scope (ex: HR, CnB,...)
	DECLARE @sqlGenerateRule NVARCHAR(MAX) = ';WITH targetWithScope AS '
		+ '(SELECT myself.EntityId, u55.ScopeId FROM rs_user.ConfigTenant.UserScope u55 JOIN (' + @myselfQuery + ') AS myself ON myself.UserId = u55.UserId ) '	
		+ 'INSERT INTO '+@generatedTableBackupName+' '
		+ 'SELECT DISTINCT us.RoleId ''SourceId'', targetWithScope.EntityId AS ''EntityId'', 1 AS SourceType, p.Permission ''Permission'' FROM '
		+ 'JplSecurity.[Grant].PolicyLink pl '
		+ 'JOIN JplSecurity.[Grant].Policy p ON p.Id = pl.PolicyId AND pl.EntityType = 1 '
		+ 'JOIN rs_user.ConfigTenant.UserScope us ON us.RoleId = pl.EntityId '
		+ 'JOIN targetWithScope ON targetWithScope.ScopeId = us.ScopeId '
		+ 'WHERE p.ServiceEntityId  = ''' + CONVERT(NVARCHAR(100),@serviceEntityId) + ''''
	
	IF (@debug = 1) PRINT ('sqlGenerateRule')
	
	DECLARE @sqlMergeData NVARCHAR(MAX) = 'MERGE INTO '+@generatedTableName+' AS tgt '
	+ 'USING '+@generatedTableBackupName+' AS src '
	+ 'ON (tgt.[SourceId]=src.[SourceId] AND tgt.[EntityId]=src.[EntityId]) AND tgt.SourceType = src.SourceType AND tgt.[Permission]=src.[Permission] '
	+ 'WHEN NOT MATCHED BY TARGET '
	+ 'THEN INSERT (SourceId, EntityId, SourceType, Permission) '
	+ 'VALUES (src.SourceId, src.EntityId, src.SourceType, src.Permission) '
	+ 'WHEN NOT MATCHED BY SOURCE '
	+ 'THEN DELETE;'
	
	IF (@debug = 1) PRINT ('exec all sql start');

	DECLARE @sqlFinal NVARCHAR(MAX) = 
	' ' + @sqlCreateTempTable
	+ '; ' + @sqlAttributeQuery
	+ '; ' + @sqlGenerateRule
	+ '; ' + @sqlMergeData

	EXEC sp_executesql @sqlCreateTable
	EXEC sp_executesql @sqlFinal


END