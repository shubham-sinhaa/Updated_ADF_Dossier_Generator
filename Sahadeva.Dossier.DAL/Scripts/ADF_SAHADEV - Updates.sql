-- Updated to fetch the template name as well
CREATE OR ALTER PROC [dbo].[USP_FetchPending_DCIDsToProcess_All]  
AS  
BEGIN   
 select cd.CDID,cd.DossierMachineNo,cdc.TemplateID, dt.TemplateName  
   FROM CoverageDossier AS cd WITH (NOLOCK)  
 LEFT JOIN ClientDossierConfig AS cdc  WITH (NOLOCK) ON cdc.CDCID = cd.CDCID  
 INNER JOIN DossierTemplates dt ON cdc.TemplateID = dt.DTID
 WHERE   StatusID=5   AND cdc.IsActive=1 --and cdc.TemplateID=1  AND cdc.ReportScheduleID=3  
 --AND cd.CDCID NOT IN (57,129)  
  
  -- SELECT cd.CDID FROM CoverageDossier AS cd WHERE cd.CDCID IN (1237,2246,2247,1216,1238,1239)  
END  