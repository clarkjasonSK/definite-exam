using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class HoleHandler : Singleton<HoleHandler>, ISingleton, IEventObserver
{
    #region ISingleton Variables
    private bool isDone = false;
    public bool IsDoneInitializing
    {
        get { return isDone; }
    }
    #endregion

    [SerializeField] private GameValues _game_values ;


    [SerializeField] private HoleStaff _hole_staff;
    public HoleStaff HoleStaff
    {
        set { _hole_staff = value; }
    }
    
    #region Cache Variables
    private Prop propRef;
    private Hole holeRef;
    private Hole holeRef2;
    #endregion


    public void Initialize()
    {
        if(_game_values == null)
            _game_values = GameManager.Instance.GameValues;

        AddEventObservers();

        isDone = true;
    }

    public void AddEventObservers()
    {
        EventBroadcaster.Instance.AddObserver(EventKeys.HOLE_LEVEL_UP, onHoleLevelUp);

        EventBroadcaster.Instance.AddObserver(EventKeys.OUTER_ENTER_PROP, onEnterOuterProp);
        EventBroadcaster.Instance.AddObserver(EventKeys.OUTER_STAY_PROP, onStayOuterProp);
        EventBroadcaster.Instance.AddObserver(EventKeys.OUTER_EXIT_PROP, onExitOuterProp);
        EventBroadcaster.Instance.AddObserver(EventKeys.INNER_ENTER_PROP, onEnterInnerProp);
        EventBroadcaster.Instance.AddObserver(EventKeys.INNER_STAY_PROP, onStayInnerProp);
        EventBroadcaster.Instance.AddObserver(EventKeys.INNER_EXIT_PROP, onExitInnerProp);

        EventBroadcaster.Instance.AddObserver(EventKeys.PROP_ABSORBED, onPropAbsorbed);

        EventBroadcaster.Instance.AddObserver(EventKeys.OUTER_ENTER_HOLE, onEnterOuterHole);
        EventBroadcaster.Instance.AddObserver(EventKeys.OUTER_EXIT_HOLE, onExitOuterHole);
        EventBroadcaster.Instance.AddObserver(EventKeys.INNER_ENTER_HOLE, onEnterInnerHole);
        EventBroadcaster.Instance.AddObserver(EventKeys.INNER_STAY_HOLE, onStayInnerHole);
        EventBroadcaster.Instance.AddObserver(EventKeys.INNER_EXIT_HOLE, onExitInnerHole);

        EventBroadcaster.Instance.AddObserver(EventKeys.HOLE_ABSORBED, onHoleAbsorbed);

    }

    private void spawnRivalHole(int level)
    {
        Hole rival = _hole_staff.HoleObjPool.GameObjectPool.Get().GetComponent<Hole>();
        rival.SetTarget(PlayerHandler.Instance.GetTargetHole);
        rival.InitializeHole(level, _hole_staff.HoleSpawners[UnityEngine.Random.Range(0, _hole_staff.HoleSpawners.Count)].transform.localPosition, new Color(0,255,0));
    }

    private void despawnRivalHole(Hole hole)
    {
        _hole_staff.HoleObjPool.GameObjectPool.Release(hole.gameObject);
    }

    private void setPropRefs(EventParameters param)
    {
        propRef = param.GetParameter<Prop>(EventParamKeys.PROP_PARAM, null);
        holeRef = param.GetParameter<Hole>(EventParamKeys.HOLE_PARAM, null);
    }

    private int setHoleRefs(EventParameters param)
    {
        holeRef = param.GetParameter<Hole>(EventParamKeys.HOLE_PARAM, null);
        holeRef2 = param.GetParameter<Hole>(EventParamKeys.HOLE_PARAM_2, null);

        //returns 1 if hole1 is bigger than hole2, no change
        if (holeRef.HoleLevel - holeRef2.HoleLevel >= _game_values.HoleAbsorbDifference)
        {
            return 1;
        }

        //returns -1 if hole 2 is bigger than hole 1 after switching;
        if (holeRef2.HoleLevel - holeRef.HoleLevel >= _game_values.HoleAbsorbDifference)
        {
            // Switch paramters so hole_param_2 is the smaller one
            holeRef = param.GetParameter<Hole>(EventParamKeys.HOLE_PARAM_2, null);
            holeRef2 = param.GetParameter<Hole>(EventParamKeys.HOLE_PARAM, null);

            param.AddParameter(EventParamKeys.HOLE_PARAM, holeRef);
            param.AddParameter(EventParamKeys.HOLE_PARAM_2, holeRef2);

            return -1;
        }

        // returns 0 if equal, no change
        return 0;
    }

    #region Prop on Hole Events

    private void onEnterOuterProp(EventParameters param)
    {
        setPropRefs(param);
        propRef.Pull(holeRef.transform);
    }
    private void onStayOuterProp(EventParameters param)
    {

    }
    private void onExitOuterProp(EventParameters param)
    {
        propRef = param.GetParameter<Prop>(EventParamKeys.PROP_PARAM, null);
        propRef.PullStop();
    }


    private void onEnterInnerProp(EventParameters param)
    {
        setPropRefs(param);

        if (propRef.PropSize <= holeRef.HoleLevel)
        {
            propRef.Absorb(param);
        }
    }
    private void onStayInnerProp(EventParameters param)
    {
        setPropRefs(param);

        if (!propRef.IsBeingAbsorbed && propRef.PropSize <= holeRef.HoleLevel)
        {
            propRef.Absorb(param);
        }
    }
    private void onExitInnerProp(EventParameters param)
    {
        if (!propRef.gameObject.activeInHierarchy)
            return;
        propRef = param.GetParameter<Prop>(EventParamKeys.PROP_PARAM, null);
        propRef.AbsorbStop();
    }


    private void onPropAbsorbed(EventParameters param)
    {
        setPropRefs(param);
        if (holeRef.IsFromObjPool)
            return;
        holeRef.AddHoleExperience(propRef.PropPoints);
        EventBroadcaster.Instance.PostEvent(EventKeys.PLAYER_SO_UPDATE, param);
    }
    #endregion

    #region Hole on Hole Events
    private void onEnterOuterHole(EventParameters param)
    {

    }
    private void onExitOuterHole(EventParameters param)
    {

    }
    private void onEnterInnerHole(EventParameters param)
    {
        if(setHoleRefs(param) != 1)
        {
            return;
        }

        holeRef2.Absorb(param);
        
    }
    private void onStayInnerHole(EventParameters param)
    {
        if(setHoleRefs(param) != 1)
        {
            return;
        }

        holeRef2.Absorb(param);
    }

    private void onExitInnerHole(EventParameters param)
    {

        if (setHoleRefs(param) != -1)
        {
            return;
        }

        if (!holeRef2.gameObject.activeInHierarchy)
        {
            return;
        }

        holeRef2.AbsorbStop();
    }
    private void onHoleAbsorbed(EventParameters param)
    {
        setHoleRefs(param);
        /*
        if (holeRef.IsFromObjPool)
        {

            Debug.Log("Rival hole absorbing");
            return;
        }*/

        holeRef.AddHoleExperience( (int)Math.Round(holeRef2.HoleExperience * _game_values.HoleExpCannibalMultiplier));

        param.AddParameter(EventParamKeys.HOLE_PARAM, holeRef);
        param.AddParameter(EventParamKeys.PLAYER_PARAM, holeRef.PlayerHole);
        EventBroadcaster.Instance.PostEvent(EventKeys.PLAYER_SO_UPDATE, param);

        if (holeRef2.PlayerHole != null)
        {
            EventBroadcaster.Instance.PostEvent(EventKeys.GAME_OVER, param);
        }
        if (holeRef2.IsFromObjPool)
        {
            despawnRivalHole(holeRef2);
        }
        holeRef2.gameObject.SetActive(false);
    }
    private void onHoleLevelUp(EventParameters param)
    {
        holeRef = param.GetParameter<Hole>(EventParamKeys.HOLE_PARAM, null);
        holeRef.IncreaseHoleSize();

        if (GameManager.Instance.CurrentBiggestHole % 3 == 0)
        {
            spawnRivalHole(GameManager.Instance.CurrentBiggestHole+1);
        }
    }
    #endregion
}
