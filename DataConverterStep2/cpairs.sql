IF OBJECT_ID (N'ORA_CABLEPAIRS', N'U') IS NOT NULL 
   drop table ORA_CABLEPAIRS ELSE SELECT 0 AS res;
    
select * into ORA_CABLEPAIRS from openquery(oracle_ncc, 'select * from msc_ncc.cablepairs');
 
 
declare @edgetype uniqueidentifier
declare @cpsstatus uniqueidentifier
declare @cpobjecttype int
select  @edgetype=s.oid from type s inner join edgetype cps on cps.oid=s.oid where typename like 'Cable Pair'
--select @edgetype

select @cpsstatus=s.oid from status s inner join CablePairStatus cps on cps.oid=s.oid where StatusName like 'Active'
--select @cpsstatus

select @cpobjecttype=s.oid from XPObjectType s  where TypeName like '%PhysicalPair' 
--select @cpobjecttype

-- (select * into ORA_CABLEPAIRS from openquery(oracle_ncc, 'select * from msc_ncc.cablepairs')

---- create baseobjects 
--begin tran
insert into dbo.BaseObjectDateTimeStamps (Oid ,IsEngineering ,CreateDate ,UpdateDate ,ExternalSystemId ,SourceTable		,SourceType ,OptimisticLockField ,GCRecord ,ObjectType)
select							newId(), 1,				getdate(), getdate(), ID,	'MSC_NCC.CablePairs', null, 0,				null,		 @cpobjecttype
from ORA_CABLEPAIRS   ora 
inner join (select ExternalSystemId from BaseObjectDateTimeStamps bo inner join cable c on bo.oid=c.oid) cabs
on cabs.ExternalSystemId = ora.cable


--edges
--	begin tran
insert into edge( Oid      ,EdgeType      ,FromNode      ,ToNode )
select bo.oid, @edgetype, null, null from 
BaseObjectDateTimeStamps bo left outer join edge e on bo.oid=e.oid where e.oid is null

--pairs		 
insert into cablepair (Oid      ,PairNumber      ,Status       ,Cable        ,Comment  )
 
--select *
  select bo.oid,ocp.num, @cpsstatus, ctable.coid, null
from 
BaseObjectDateTimeStamps bo
left outer join cablepair   on bo.oid=cablepair.oid
inner join ORA_CABLEPAIRS ocp on ocp.ID=bo.ExternalSystemId
inner join ( select externalsystemid as cid, bo2.oid as coid from BaseObjectDateTimeStamps bo2 inner join cable  on bo2.oid=cable.Oid )  ctable
on ocp.Cable=ctable.cid 
where bo.ObjectType=@cpobjecttype and cablepair.oid is null and ocp.cable is not null


insert into physicalpair(oid)
select oid from cablepair where oid not in (select oid from physicalpair)



update cable set status = (select cs.oid from cablestatus cs inner join status s on cs.oid=s.oid where statusname like 'Unknown') where status is null

