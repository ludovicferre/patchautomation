CREATE PROCEDURE [dbo].[CWoC_GetAllBulletins]
    @Vendor_Name varchar(255)     = '%'
AS
BEGIN
    SELECT      swb.BulletinGuid        AS [_ResourceGuid],
                rsb.[Name]              AS [Bulletin],      -- Bulletin Name
                asr.SeverityName
                                        AS [Severity],
				swb.Name as 'Vendor',
                ISNULL( csr.SeverityName, 'No Set' )
                                        AS [Custom Severity],
                CASE WHEN ( swb.Active = 1 AND swb.Updates = swb.Downloaded )
                     THEN 'Yes'
                     ELSE 'No'
                 END                    AS [Downloaded],
                ISNULL( tsk.Tasks, 0 )  AS [Policies],
                swb.Updates,
                swb.Downloaded AS [Available Packages],
                swb.FirstReleaseDate    AS [Released],
                swb.LastRevisionDate    AS [Revised],
                rsb.[Description]       -- Bulletin Description
        FROM  ( SELECT      isb._ResourceGuid               AS [BulletinGuid],
                            isb.FirstReleaseDate,
                            isb.LastRevisionDate,
                            act.Enabled                     AS [Active],
                            COUNT(b2u.ChildResourceGuid)    AS [Updates],
                            COUNT(u2p.ChildResourceGuid)    AS [Downloaded],
							v.name
                    FROM    Inv_Software_Bulletin  isb
                    JOIN    ItemActive             act ON act.Guid = isb._ResourceGuid
                    JOIN    ResourceAssociation    b2v ON b2v.ParentResourceGuid = isb._ResourceGuid
                              AND b2v.ResourceAssociationTypeGuid = '2FFEB9F0-601E-4746-A830-BDB200076503' -- SWB to Vendor
                    JOIN    ResourceAssociation    b2u ON b2u.ParentResourceGuid = isb._ResourceGuid
					JOIN RM_ResourceCompany v
 										  ON v.Guid = b2v.ChildResourceGuid
                                                      AND b2u.ResourceAssociationTypeGuid = '7EEAB03A-839C-458d-9AF2-55DB6B173293' -- SWB to SWU
                    JOIN vPMCore_UpdatePlatform plat  
						          ON  plat.UpdateGuid = b2u.ChildResourceGuid
                  LEFT JOIN ResourceAssociation    u2p ON u2p.ParentResourceGuid = b2u.ChildResourceGuid
                                                      AND u2p.ResourceAssociationTypeGuid = 'A19CED33-9E1F-4E97-98CF-0F8B339739C3' -- SWU to SWPackage
                    WHERE   isb.LastRevisionDate > DATEADD( yy, -24, GETDATE())
                   GROUP BY isb._ResourceGuid, isb.FirstReleaseDate, isb.LastRevisionDate, act.Enabled, v.name
              ) AS                           swb
        JOIN    RM_ResourceSoftware_Bulletin rsb ON rsb.Guid = swb.BulletinGuid
        JOIN    vPMCore_SeverityRating       asr ON asr._ResourceGuid = swb.BulletinGuid    -- Severity Rating Joins
      LEFT JOIN vPMCore_SeverityRating       csr ON csr._ResourceGuid = swb.BulletinGuid    -- Custom Severity Rating
                                                AND csr.SeverityRatingSystemGuid = '6CCEF81F-F791-4DC4-8FC6-90D149FC0187'
      LEFT JOIN vPMCore_TasksPerBulletin     tsk ON tsk.BulletinGuid  = swb.BulletinGuid
        WHERE   asr.ProviderGuid <> 'E2FEA34C-ADBB-47BD-9D7A-1092C5078245' -- do not show row of Severity with Altiris provider  (fix for #1910916 )
          AND swb.Name like @Vendor_Name
      ORDER BY  Bulletin DESC
END

GO
CREATE PROCEDURE [dbo].[CWoC_GetBulletins_Google]
    @Vendor_Name varchar(255)     = '%'
AS
BEGIN
	exec CWoC_GetAllBulletins 'Google'
END

GO
CREATE PROCEDURE [dbo].[CWoC_GetBulletins_Mozilla]
    @Vendor_Name varchar(255)     = '%'
AS
BEGIN
	exec CWoC_GetAllBulletins 'Mozilla'
END

GO
-- Test execution
exec CWoC_GetBulletins_Google
exec CWoC_GetBulletins_Mozilla