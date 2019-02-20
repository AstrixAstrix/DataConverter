IF OBJECT_ID (N'ORA_DESIGNATIONPAIRS', N'U') IS NOT NULL 
   drop table ORA_DESIGNATIONPAIRS ELSE SELECT 0 AS res;
    
select * into ORA_DESIGNATIONPAIRS from openquery(oracle_ncc, 'select * from msc_ncc.designationpairs');
 
 
declare @edgetype uniqueidentifier
declare @cpsstatus uniqueidentifier
declare @cpobjecttype int
select  @edgetype=s.oid from type s inner join edgetype cps on cps.oid=s.oid where typename like 'Cable Pair'
--select @edgetype

select @cpsstatus=s.oid from status s inner join CablePairStatus cps on cps.oid=s.oid where StatusName like 'Active'
--select @cpsstatus

select @cpobjecttype=s.oid from XPObjectType s  where TypeName like '%DesignationPair' 
--select @cpobjecttype

-- (select * into ORA_DESIGNATIONPAIRS from openquery(oracle_ncc, 'select * from msc_ncc.DESIGNATIONpairs')

---- create baseobjects 
--begin tran
insert into dbo.BaseObjectDateTimeStamps (Oid ,IsEngineering ,CreateDate ,UpdateDate ,ExternalSystemId ,SourceTable		,SourceType ,OptimisticLockField ,GCRecord ,ObjectType)
select							newId(), 1,				getdate(), getdate(), ID,	'MSC_NCC.DESIGNATIONPairs', 'sqlscript', 0,				null,		 @cpobjecttype
from ORA_DESIGNATIONPAIRS   ora 
inner join (select ExternalSystemId from BaseObjectDateTimeStamps bo inner join DesignationGroup c on bo.oid=c.oid) cabs
on cabs.ExternalSystemId = ora.DGROUP


--edges
--	begin tran
insert into edge( Oid      ,EdgeType      ,FromNode      ,ToNode )
select bo.oid, @edgetype, null, null from 
BaseObjectDateTimeStamps bo left outer join edge e on bo.oid=e.oid where e.oid is null

--pairs		 
insert into DESIGNATIONpair (Oid      ,PairNumber      ,Status       ,DesignationGroup        ,Comment  )
 
--select *
select bo.oid,ocp.count, @cpsstatus, ctable.coid, 'fromscript'
from 
BaseObjectDateTimeStamps bo
left outer join DESIGNATIONpair   on bo.oid=DESIGNATIONpair.oid
inner join ORA_DESIGNATIONPAIRS ocp on ocp.ID=bo.ExternalSystemId
inner join ( select externalsystemid as cid, bo2.oid as coid from BaseObjectDateTimeStamps bo2 inner join DesignationGroup  on bo2.oid=DesignationGroup.Oid )  ctable
on ocp.DGROUP=ctable.cid 
where bo.ObjectType=@cpobjecttype and DESIGNATIONpair.oid is null and ocp.Dgroup is not null

 