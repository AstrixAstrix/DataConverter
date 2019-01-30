--viewls
---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
--CABLEPAIRDESIGNATIONPAIR-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
  
  CREATE OR REPLACE FORCE VIEW "MSC_KALONA"."CABLEPAIRDESIGNATIONPAIR" ("PAIRID", "COUNT", "STATUS", "CABLEID", "TU_ID", "LOGICALCOUNTID", "DESIGNATIONGROUPID", "LOGICALCOUNT", "DESIGNATIONGROUPNAME") AS 
  Select  F.ID as PairId, F.NUM as COUNT, F.STATUS as STATUS, F.CABLE_SEG_ID as CableId, TU.ID as TU_ID, tu.cr_logical_count_id as LogicalCountId, 
tu.cr_logical_cable_id as DESIGNATIONGROUPID, LCC.COUNT as LogicalCount, lc.name as DESIGNATIONGROUPNAME
--select *
from MSC_KALONA.CR_TRANSMISSION_UNIT TU INNER JOIN
MSC_KALONA.fiber F on F.ID=tu.obj_ref_id  INNER JOIN
MSC_KALONA.CR_LOGICAL_COUNT LCC on LCC.ID=tu.cr_logical_count_id LEFT OUTER JOIN
MSC_KALONA.CR_LOGICAL_CABLE lC ON LC.ID=lcc.cr_logical_cable_id inner join
CR_TRANSMISSION_MEAN TM ON
TM.ID=TU.CR_TRANSMISSION_MEAN_ID
---WHERE F.CABLE_SEG_ID=6808
ORDER BY F.NUM;

---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
--CABLEPAIRS-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

  CREATE OR REPLACE FORCE VIEW "MSC_KALONA"."CABLEPAIRS" ("ID", "NUM", "STATUS", "CABLE") AS 
  Select f.ID, f.NUM, f.STATUS,f.Cable_SEG_ID as CABLE  from MSC_KALONA.FIBER f;-- inner join cable_seg cs on  cs.id=f.cable_seg_id;;

---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
--CABLES-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

  CREATE OR REPLACE FORCE VIEW "MSC_KALONA"."CABLES" ("FORC", "CABLEID", "CABLESTATUS", "CABLELENGTH", "CABLEROUTE", "WORKORDERID", "DROPCABLE", "CABLETYPE", "CABLECLASS", "CABLESIZE", "SOURCELOCATIONID", "DESTINATIONLOCATIONID", "DESCRIPTION", "INSTALLDATE") AS 
  SELECT  CASE "FOC" WHEN 1 then 'COPPER' ELSE 'FIBER' END as FORC
,"CABLEID","CABLESTATUS","CABLELENGTH","CABLEROUTE","WORKORDERID","DROPCABLE","CABLETYPE","CABLECLASS","CABLESIZE","SOURCELOCATIONID","DESTINATIONLOCATIONID","DESCRIPTION","INSTALLDATE"
from (Select ct.Cable_type as FOC, cs.ID as CableId, cs.status as CableStatus, cs.total_real_length as CableLength, cs.transport_route as CableRoute, cs.place_wo_id as WorkOrderId, cs.drop_cable as DropCable ,ct.STD_CODE as CableType, 
FIBER_TYPE as CableClass, Capacity as CableSize, aps.acc_point_id as SourceLocationId, apd.acc_point_id as DestinationLocationId,
cs.Rus_Suffix as Description, crp.Build_Date as INSTALLDATE
FROM MSC_KALONA.CABLE_SEG CS LEFT OUTER  JOIN 
MSC_KALONA.CABLE_TYPE ct on ct.id=cs.cable_type left outer JOIN
MSC_KALONA.op_cable_span csp on csp.op_cable_seg_id=cs.id left outer JOIN
MSC_KALONA.acc_point aps on csp.op_acc_point_id_1=aps.id left outer JOIN
MSC_KALONA.ACC_POINT APD ON CSP.OP_ACC_POINT_ID_2=APD.ID LEFT OUTER JOIN
CR_PROJECT CRP ON  CS.PLACE_WO_ID=CRP.ID) X;

---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
--CONDUITs-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

  CREATE OR REPLACE FORCE VIEW "MSC_KALONA"."CONDUITS" ("ID", "STATUS", "LENGTH", "TYPE", "CODE", "MEDIA", "WORKORDER", "CABLE", "INSTALLDATE") AS 
  Select c.id as ID,c.status,c.cc_length as Length,ct.std_code as TYPE, ct.Diameter as Code,ct.Material as MEDIA, c.place_wo_id as Workorder,  ccs.cable_seg_id as Cable, crp.Build_Date as INSTALLDATE  from MSC_KALONA.Conduit c
left join   MSC_KALONA.CONDUIT_FORMATION cf on cf.conduit_id=c.id 
left join bore b on cf.id=b.conduit_formation_id
left join Conduit_cable_seg ccs on c.id=ccs.conduit_id
left join cond_type ct on cf.cond_type_id=ct.id  left outer join
cr_project crp on  c.place_wo_id=crp.id;

---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
--DESIGNATIONGROUPS-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

  CREATE OR REPLACE FORCE VIEW "MSC_KALONA"."DESIGNATIONGROUPS" ("DGID", "DGNAME", "STATUS", "CODE", "SOURCE") AS 
  SELECT DG.ID AS DGID, DG.NAME AS DGNAME  ,DG.STATUS AS STATUS ,Code as Code,CS.CR_ACC_POINT_ID AS SOURCE   
    FROM MSC_KALONA.CR_LOGICAL_CABLE DG 
LEFT OUTER JOIN CR_SITE CS ON CS.ID= DG.CR_SITE_ID;

---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
--DESIGNATIONPAIRS-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

  CREATE OR REPLACE FORCE VIEW "MSC_KALONA"."DESIGNATIONPAIRS" ("ID", "COUNT", "DGROUP") AS 
  Select id, count, cr_logical_cable_id as DGROUP  from
MSC_KALONA.CR_LOGICAL_COUNT;

---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
--SUBSCRIBERS-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

  CREATE OR REPLACE FORCE VIEW "MSC_KALONA"."SUBSCRIBERS" ("SUBSCRIBERID", "ACC_POINT_ID_FLEXINT", "SUBSCRIBERNAME", "SUBSCRIBERTYPE", "SUBSCRIBERSTATUS", "SUBSCRIBERCODE", "ADDRESSHOUSENUM", "ADDRESSPARCELID", "ADDRESSSTREET", "ADDRESSSTREETTYPE", "SUBSCRIBERREGIONNAME", "ADDRESSZIP", "CITY", "FULLADDY") AS 
  Select sh.ID as SubscriberId, 
ap.acc_point_id as acc_point_id_FlexInt,
  sh.code as SubscriberName, 
 case stet.description when 'Custumer Premise' then 'Customer Premise' else stet.Description end as SubscriberType,
  sh.Status as SubscriberStatus,
  sh.code as SubscriberCode, 
  l.Num as AddressHouseNum  , 
  L.COMMENTS as AddressParcelId, 
S.name as AddressStreet,
st.str_type as AddressStreetType, 
R.NAME SubscriberRegionName,
Z.ZIP_CODE as AddressZip,
case scnl when 'WASH' then 'Washington'   when 'RVRS' then 'Riverside' else   'Kalona' end as CITY,
concat(l.num , concat( ' ' , concat(s.name  , concat(' ' ,concat( st.str_type ,  ' '  ))))) as FULLADDY 
from MSC_KALONA.site_holder sh left join
MSC_KALONA.LOT L on l.id=sh.LB_LOT_ID left Join
MSC_KALONA.STREET S on s.id=sh.LB_STREET_ID left join
MSC_KALONA.Region R on r.id=sh.lb_region_id  left join 
MSC_KALONA.Zip Z on z.id=l.zip_id left join
MSC_KALONA.acc_point ap on sh.id=ap.acc_point_id left join
MSC_KALONA.str_type st on st.id=s.str_type left outer join
 MSC_KALONA.CR_SITE ste on ste.cr_acc_point_id=ap.acc_point_id left outer join 
 MSC_KALONA.CR_SITE_TYPE STET ON STET.ID=CR_SITE_TYPE_ID;

---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
--TERMINALS-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

  CREATE OR REPLACE FORCE VIEW "MSC_KALONA"."TERMINALS" ("OBJECTID", "APID", "STATUS", "NAME", "WO", "TYPE", "CITY", "INSTALLDATE") AS 
  SELECT PED.ID AS objectid, 
  LOT_ID AS APID, 
  PED.STATUS ,
  ped.num as name ,
  PED.PLACE_WO_ID AS WO ,
  PED.PEDESTAL_TYPE_ID AS TYPE,
  SITE.NAME as city ,
  crp.build_date as installdate
FROM MSC_KALONA.PEDESTAL ped
INNER JOIN MSC_KALONA.PEDESTAL_TYPE pedtype
ON pedtype.ID=ped.Pedestal_Type_id
LEFT OUTER JOIN region site
ON site.id = ped.region_id
LEFT OUTER JOIN CR_PROJECT CRP
ON PED.PLACE_WO_ID=CRP.ID
-------------------------------------------------------------------------------------
--UNION-----------------------------------------------------------------------------------
-------------------------------------------------------------------------------------
--SELECT ap.ACC_POINT_ID AS accesspointId,
--  ap.id                AS APID,
--  T.id                 AS TerminalId,
--  T.status             AS Status,
--  T.Name               AS Name,
--  T.Route              AS Route,
--  t.place_wo_id        AS WorkOrderId,
--  OP_TERMINAL_TYPE_ID  AS Type,
--  site.Name            AS CITY, --, rus_suffix as description
--  crp.build_date       AS INSTALLDATE
--FROM MSC_KALONA.acc_point ap
--INNER JOIN MSC_KALONA.OP_TERMINAL T
--ON t.acc_point_id=ap.id
--LEFT OUTER JOIN region site
--ON site.id = lb_region_id
--LEFT OUTER JOIN CR_PROJECT CRP
--ON t.place_wo_id=crp.id
-----------------------------------------------------------------------------------
UNION-----------------------------------------------------------------------------------
-----------------------------------------------------------------------------------
SELECT ped.Id ,
  LOT_ID , 
  ped.Status ,
  ped.num , 
  ped.place_wo_id ,
  ped.HAND_HOLE_type_id ,
  site.Name ,
  CRP.BUILD_DATE
FROM MSC_KALONA.HAND_HOLE ped
INNER JOIN MSC_KALONA.HAND_HOLE_TYPE pedtype
ON pedtype.ID=ped.HAND_HOLE_Type_id
LEFT OUTER JOIN region site
ON site.id = ped.region_id
LEFT OUTER JOIN CR_PROJECT CRP
ON PED.PLACE_WO_ID=CRP.ID
-----------------------------------------------------------------------------------
UNION-----------------------------------------------------------------------------------
-----------------------------------------------------------------------------------
SELECT 
ped.Id ,
  LOT_ID , 
  ped.Status ,
  ped.num , 
  ped.place_wo_id ,
  ped.MANHOLE_type_id ,
  site.Name ,
  CRP.BUILD_DATE
FROM MSC_KALONA.MANHOLE ped
INNER JOIN MSC_KALONA.MANHOLE_TYPE pedtype
ON pedtype.ID=ped.MANHOLE_Type_id
LEFT OUTER JOIN region site
ON site.id = ped.region_id
LEFT OUTER JOIN CR_PROJECT CRP
ON PED.PLACE_WO_ID=CRP.ID
-----------------------------------------------------------------------------------
UNION-----------------------------------------------------------------------------------
-----------------------------------------------------------------------------------
SELECT
 ped.Id ,
  LOT_ID , 
  ped.Status ,
  ped.num , 
  ped.place_wo_id ,
  PED.POLE_TYPE_ID ,
  crp.Code,
 -- SITE.NAME ,
  Initial_Date
FROM MSC_KALONA.POLE PED
LEFT JOIN MSC_KALONA.POLE_TYPE pedtype
ON pedtype.ID=ped.POLE_Type_id 
LEFT OUTER JOIN CR_PROJECT CRP
ON PED.PLACE_WO_ID=CRP.ID;

---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
--JUNCTIONS_EXT-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

  CREATE OR REPLACE FORCE VIEW "MSC_KALONA"."JUNCTIONS_EXT" ("INSTALLDATE_NEW", "ENTITYID", "ENTITYTYPE", "ACCESSPOINTTYPE", "ACCESSPOINTID", "REFERENCENAME", "REFERENCETYPECODE", "ENTITYNAME", "REGIONCODE", "SUBTYPE", "WOID", "ROUTE") AS 
  Select cp.INSTALLATION_DATE as INSTALLDATE_NEW, ap."ENTITYID",ap."ENTITYTYPE",ap."ACCESSPOINTTYPE",ap."ACCESSPOINTID",ap."REFERENCENAME",ap."REFERENCETYPECODE",ap."ENTITYNAME",ap."REGIONCODE",ap."SUBTYPE",ap."WOID", Route from (SELECT acc_point.id as ENTITYID, 'POLE' as ENTITYTYPE
          , acc_point.ap_type as ACCESSPOINTTYPE
          , acc_point.acc_point_id as ACCESSPOINTID
          , acc_point.reference_name as REFERENCENAME
          , reference_type.code as REFERENCETYPECODE
          , pole.num as ENTITYNAME
          ,LB_REGION_ID as REGIONCODE
          ,POLE_TYPE.CODE as SUBTYPE
          ,PLACE_WO_ID as WOID
FROM MSC_KALONA.acc_point left outer join MSC_KALONA.reference_type on acc_point.reference_type_id = reference_type.id
        inner join MSC_KALONA.POLE on  acc_point.acc_point_id      = pole.id LEFT OUTER join POLE_TYPE on POLE.POLE_TYPE_ID=POLE_TYPE.ID
WHERE  acc_point.ap_type           = 1
UNION ALL
SELECT acc_point.id, 'PEDESTAL'
          , acc_point.ap_type
          , acc_point.acc_point_id
          , acc_point.reference_name
          , reference_type.code
          , TO_CHAR(pedestal.num)
          , region.cnl
           ,PEDESTAL_TYPE.CODE as SUBTYPE
          ,PLACE_WO_ID as WOID
FROM MSC_KALONA.acc_point left outer join MSC_KALONA.reference_type on acc_point.reference_type_id = reference_type.id
        inner join MSC_KALONA.pedestal on  acc_point.acc_point_id      = pedestal.id left outer join MSC_KALONA.region on region.id=pedestal.region_id
        LEFT OUTER join PEDESTAL_TYPE on PEDESTAL.PEDESTAL_TYPE_ID=PEDESTAL_TYPE.ID
WHERE  acc_point.ap_type           = 2
UNION ALL
SELECT acc_point.id, 'HANDHOLE'
          , acc_point.ap_type
          , acc_point.acc_point_id
          , acc_point.reference_name
          , reference_type.code
          , hand_hole.num
          , region.cnl
          ,HAND_HOLE_TYPE.CODE as SUBTYPE
          ,PLACE_WO_ID as WOID

FROM MSC_KALONA.acc_point left outer join MSC_KALONA.reference_type on acc_point.reference_type_id = reference_type.id
        inner join MSC_KALONA.hand_hole on  acc_point.acc_point_id      = hand_hole.id left outer join MSC_KALONA.region on region.id=hand_hole.region_id
         LEFT OUTER join HAND_HOLE_TYPE on HAND_HOLE.HAND_HOLE_TYPE_ID=HAND_HOLE_TYPE.ID
WHERE  acc_point.ap_type           = 3
UNION ALL
SELECT acc_point.id, 'FIBER_PED'
          , acc_point.ap_type
          , acc_point.acc_point_id
          , acc_point.reference_name
          , reference_type.code
          , fiber_pedestal.num
          , region.cnl
          ,NULL as SUBTYPE
          ,NULL as WOID

FROM MSC_KALONA.acc_point left outer join MSC_KALONA.reference_type on acc_point.reference_type_id = reference_type.id
        inner join MSC_KALONA.fiber_pedestal on  acc_point.acc_point_id      = fiber_pedestal.id left outer join MSC_KALONA.region on region.id=fiber_pedestal.region_id
WHERE  acc_point.ap_type           = 5
UNION ALL
SELECT acc_point.id, 'MANHOLE'
          , acc_point.ap_type
          , acc_point.acc_point_id
          , acc_point.reference_name
          , reference_type.code
          , manhole.num
          , region.cnl
           ,MANHOLE_TYPE.CODE as SUBTYPE
          ,PLACE_WO_ID as WOID

FROM MSC_KALONA.acc_point left outer join MSC_KALONA.reference_type on acc_point.reference_type_id = reference_type.id
        inner join MSC_KALONA.manhole on  acc_point.acc_point_id      = manhole.id left outer join MSC_KALONA.region on region.id=manhole.region_id
        LEFT OUTER join MANHOLE_TYPE on MANHOLE.MANHOLE_TYPE_ID=MANHOLE_TYPE.ID
WHERE  acc_point.ap_type           = 6
UNION ALL
SELECT acc_point.id, 'ANCHOR'
          , acc_point.ap_type
          , acc_point.acc_point_id
           , acc_point.reference_name
           , reference_type.code
           , anchor.code
           , NULL
          ,ANCHOR_TYPE.CODE as SUBTYPE
          ,PLACE_WO_ID as WOID

FROM MSC_KALONA.acc_point left outer join MSC_KALONA.reference_type on acc_point.reference_type_id = reference_type.id
        inner join MSC_KALONA.anchor on  acc_point.acc_point_id      = anchor.id 
        LEFT OUTER join ANCHOR_TYPE on ANCHOR.ANCHOR_TYPE_ID=ANCHOR_TYPE.ID
WHERE  acc_point.ap_type           = 8
UNION ALL
SELECT acc_point.id, 'CONDUIT_TAP'
          , acc_point.ap_type
           , acc_point.acc_point_id
           , acc_point.reference_name
           , reference_type.code
           , NULL
           , NULL
            ,NULL as SUBTYPE
          ,PLACE_WO_ID as WOID

FROM MSC_KALONA.acc_point left outer join MSC_KALONA.reference_type on acc_point.reference_type_id = reference_type.id
        inner join MSC_KALONA.op_conduit_tap on  acc_point.acc_point_id      = op_conduit_tap.id 
WHERE  acc_point.ap_type           = 16
/*UNION ALL
SELECT acc_point.id , 'SUBSCRIBER',
            acc_point.ap_type ,
            acc_point.acc_point_id ,
            acc_point.reference_name ,
            reference_type.code ,
            cr_acc_point.code ,
            region.cnl,
           NULL as SUBTYPE,
          NULL as WOID
            
FROM MSC_KALONA.acc_point left outer join MSC_KALONA.reference_type on acc_point.reference_type_id = reference_type.id
        inner join MSC_KALONA.cr_acc_point on  acc_point.acc_point_id = cr_acc_point.id left outer join MSC_KALONA.region on  cr_acc_point.lb_region_id     = region.id
        left outer join MSC_KALONA.site_holder on acc_point.acc_point_id        = site_holder.id
WHERE 
                 acc_point.ap_type             = 0
                
                AND cr_acc_point.ftype            = 'site holder'*/
  UNION ALL
     SELECT acc_point.id, 'GENERIC'
             
              , acc_point.ap_type
              , acc_point.acc_point_id
              , acc_point.reference_name
              , reference_type.code
              , NULL
              , NULL
              , NULL
              , NULL

FROM MSC_KALONA.acc_point left outer join MSC_KALONA.reference_type on acc_point.reference_type_id = reference_type.id
        inner join MSC_KALONA.other on  acc_point.acc_point_id      = other.id 
where acc_point.ap_type           IN (4,11,13)
UNION ALL
     SELECT acc_point.id, 'X-BOX'
              , acc_point.ap_type
              , acc_point.acc_point_id
              , acc_point.reference_name
              , reference_type.code
              , NULL
              , NULL
             , NULL
              , NULL
FROM MSC_KALONA.acc_point left outer join MSC_KALONA.reference_type on acc_point.reference_type_id = reference_type.id
        inner join MSC_KALONA.X_BOX_BASE on  acc_point.acc_point_id      = X_BOX_BASE.id 
where acc_point.ap_type=7
UNION ALL
  SELECT acc_point.id, 'WALL_ATTACHMENT'
              , acc_point.ap_type
              , acc_point.acc_point_id
              , acc_point.reference_name
              , reference_type.code
              , NULL
              , NULL
               ,WALL_ATTACHMENT_TYPE.CODE as SUBTYPE
              ,PLACE_WO_ID as WOID
FROM MSC_KALONA.acc_point left outer join MSC_KALONA.reference_type on acc_point.reference_type_id = reference_type.id
        inner join MSC_KALONA.WALL_ATTACHMENT on  acc_point.acc_point_id      = WALL_ATTACHMENT.id 
          LEFT OUTER join WALL_ATTACHMENT_TYPE on WALL_ATTACHMENT.WALL_ATTACHMENT_TYPE_ID=WALL_ATTACHMENT_TYPE.ID
where acc_point.ap_type=10   
UNION ALL
SELECT acc_point.id, 'TZONE'
              , acc_point.ap_type
              , acc_point.acc_point_id
              , acc_point.reference_name
              , reference_type.code
              , NULL
              , NULL
              ,NULL
              ,NULL
FROM MSC_KALONA.acc_point left outer join MSC_KALONA.reference_type on acc_point.reference_type_id = reference_type.id
        inner join MSC_KALONA.T_ZONE on  acc_point.acc_point_id      = T_ZONE.id 
where acc_point.ap_type=12 ) ap left outer join                                                                                                 
       MSC_KALONA.OP_TERMINAL T   ON     AP.ENTITYID  = T.ACC_POINT_ID
LEFT OUTER JOIN CR_PROJECT CP ON AP."WOID"=CP.ID;

---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
--JUNCTIONS-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

  CREATE OR REPLACE FORCE VIEW "MSC_KALONA"."JUNCTIONS" ("OBJECTID", "INSTALLDATE_NEW", "APID", "STATUS", "NAME", "WO", "TYPE", "CITY", "INSTALLDATE", "ENTITYID", "ENTITYTYPE", "ACCESSPOINTTYPE", "ACCESSPOINTID", "REFERENCENAME", "REFERENCETYPECODE", "ENTITYNAME", "REGIONCODE", "SUBTYPE", "WOID", "ROUTE") AS 
  SELECT 
"OBJECTID","INSTALLDATE_NEW","APID","STATUS","NAME","WO","TYPE","CITY","INSTALLDATE","ENTITYID","ENTITYTYPE","ACCESSPOINTTYPE","ACCESSPOINTID","REFERENCENAME","REFERENCETYPECODE","ENTITYNAME","REGIONCODE","SUBTYPE","WOID","ROUTE"
 FROM TERMINALS T 
 
left join junctions_ext jex on JEX.ACCESSPOINTID= T.ObjectId;

---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
