using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Hole : Poolable, IAbsorbable
{
    #region Hole Variables
    [SerializeField] private HoleData _hole_data;
    [SerializeField] private HoleController _hole_controller;

    [SerializeField] private Collider2D _outer_collider;
    [SerializeField] private Collider2D _inner_collider;

    public bool IsFromObjPool
    {
        get { return poolOrigin == null ? false : true; }
    }
    public int HoleLevel
    {
        get{ return _hole_data.HoleLevel; }
    }
    public int HoleExperience
    {
        get { return _hole_data.HoleExperience; }
    }
    public int HoleNxtLvl
    {
        get { return _hole_data.HoleCurrentExpThreshold; }
    }
    #endregion

    [SerializeField] private GameValues _game_values;
    [SerializeField] private Player _playerHole; // optional if just a hole, required if has player component

    public Player PlayerHole
    {
        get { return _playerHole; }
    }

    [SerializeField] private Hole _target_hole;


    #region EventParameters
    EventParameters holeParams;
    #endregion

    #region Coroutines
    IEnumerator _growing_hole;
    private IEnumerator _absorbing;
    private IEnumerator _stop_absorbing;
    #endregion

    private float holeScale;
    private bool _is_absorbing;
    private Vector3 tempVec;

    void Start()
    {
        if (IsFromObjPool)
            return;

        InitializeHole(1, this.transform.localPosition, new Color(255, 0, 0));
        holeParams.AddParameter(EventParamKeys.PLAYER_PARAM, _playerHole);
    }

    public void InitializeHole(int level, Vector3 spawnLocation, Color color)
    {
        if (_game_values == null)
            _game_values = GameManager.Instance.GameValues;
        if (_playerHole == null)
            _playerHole = GetComponentInChildren<Player>();

        if (_hole_data == null)
        {
            _hole_data = GetComponent<HoleData>();
        }

        this.transform.localPosition = spawnLocation;

        _hole_data.InitializeHoleData(level, color, _game_values.HoleExpBaseThreshold, _game_values.HoleRivalExpMultiplier);

        if (level == 1)
        {
            _hole_data.HoleCurrentExpThreshold = _game_values.HoleExpBaseThreshold;
            holeScale = _game_values.HoleBaseSize;
            this.transform.localScale = new Vector3(holeScale, holeScale, 1);
        }
        else
        {
            _hole_data.HoleCurrentExpThreshold = 99999;
            IncreaseHoleSize();
        }

        Physics2D.IgnoreCollision(_outer_collider, _inner_collider);
        Physics2D.IgnoreCollision(_outer_collider, PropHandler.Instance.PropStaff.PropHelper.PropSpawnBoundsCollider);
        Physics2D.IgnoreCollision(_inner_collider, PropHandler.Instance.PropStaff.PropHelper.PropSpawnBoundsCollider);

        holeParams = new EventParameters();
        holeParams.AddParameter(EventParamKeys.HOLE_PARAM, this);

    }
    void Update()
    {
        if (poolOrigin == null) // no obj pool origin, meaning it's controlled by a player
            return;

        moveHole(_target_hole.transform.localPosition, _game_values.HoleRivalSpeed);
    }

    private void moveHole(Vector2 targetLocation, float moveSpeed)
    {
        tempVec = getDirection(targetLocation);
        this.transform.position = new Vector2(transform.localPosition.x + (tempVec.x*moveSpeed * Time.deltaTime), transform.localPosition.y + (tempVec.y*moveSpeed*Time.deltaTime));
    }

    private Vector3 getDirection(Vector2 targetLocation)
    {
        return Vector3.Normalize(new Vector3(targetLocation.x - transform.localPosition.x, targetLocation.y - transform.localPosition.y));
    }
    public void SetTarget(Hole targetHole)
    {
        this._target_hole = targetHole;
    }

    public void AddHoleExperience(int exp)
    {
        if( _hole_data.AddHoleExp(exp, _game_values.HoleExpBaseThreshold, _game_values.HoleExpMultiplier))
        {
            EventBroadcaster.Instance.PostEvent(EventKeys.HOLE_LEVEL_UP, holeParams);
        }
    }
    public void IncreaseHoleSize()
    {
        holeScale = _hole_data.HoleLevel * _game_values.HoleBaseSize * _game_values.HoleSizeMultiplier;

        _growing_hole = growHole(holeScale);
        StartCoroutine(_growing_hole);
    }
    private IEnumerator growHole(float targetSize)
    {
        float tempVect = 0;
        while (this.transform.localScale.x < targetSize)
        {
            tempVect = Mathf.MoveTowards(this.transform.localScale.x, targetSize, _game_values.HoleGrowSpeed * Time.deltaTime);
            this.transform.localScale = new Vector3(tempVect, tempVect, 1);
            yield return null;
        }

        StopCoroutine("growHole");
    }


    #region IAbsorbable
    public void Absorb(EventParameters param)
    {
        if (!this.gameObject.activeInHierarchy || _is_absorbing)
            return;

        if (_stop_absorbing is not null)
            StopCoroutine(_stop_absorbing);

        _absorbing = Absorbing(param);
        StartCoroutine(_absorbing);
    }
    public IEnumerator Absorbing(EventParameters param)
    {
        _is_absorbing = true;
        
        while (this.transform.localScale.x > _game_values.HoleScaleDespawn)
        {
            this.transform.localScale = Vector2.Lerp(this.transform.localScale, Vector2.zero, _game_values.HoleAbsorbStrengthHole * Time.deltaTime);

            yield return null;
        }

        // TEMPORARY, EVENTUALLY FIND A WAY TO NOT HAVE TO GET PARAMS FROM HANDLER
        EventBroadcaster.Instance.PostEvent(EventKeys.HOLE_ABSORBED, param);

        _absorbing = null;
        _is_absorbing = false;
        yield break;
    }

    public void AbsorbStop()
    {
        if (!this.gameObject.activeInHierarchy || _absorbing == null)
        {
            return;
        }

        if (!_is_absorbing)
        {
            return;
        }

        StopCoroutine(_absorbing);

        _stop_absorbing = StopAbsorbing();
        StartCoroutine(_stop_absorbing);
    }
    public IEnumerator StopAbsorbing()
    {
        float current = 0;

        while (this.transform.localScale.x < holeScale)
        {
            current = Mathf.MoveTowards(current, 1, _game_values.HoleAbsorbStrengthHole * Time.deltaTime);
             this.transform.localScale = Vector2.Lerp(this.transform.localScale, new Vector2(holeScale, holeScale), current);
            yield return null;
        }
        _is_absorbing = false;
        _stop_absorbing = null;
        yield break;
    }

    #endregion


    private void addPropParam(Collider2D collision)
    {
        holeParams.AddParameter(EventParamKeys.PROP_PARAM, collision.transform.parent.GetComponent<Prop>());
    }
    private void addHoleParam(Collider2D collision)
    {
        holeParams.AddParameter(EventParamKeys.HOLE_PARAM, this);

        holeParams.AddParameter(EventParamKeys.HOLE_PARAM_2, collision.transform.parent.GetComponent<Hole>());
    }

    #region Enter Collider Methods ==============================================
    public void EnterColliderProp(Collider2D collision, string colliderType)
    {
        addPropParam(collision);

        switch (colliderType)
        {
            case HoleDictionary.OUTER_COLLIDER:
                EventBroadcaster.Instance.PostEvent(EventKeys.OUTER_ENTER_PROP, holeParams);
                break;
            case HoleDictionary.INNER_COLLIDER:
                EventBroadcaster.Instance.PostEvent(EventKeys.INNER_ENTER_PROP, holeParams);
                break;
        }
    }

    public void EnterColliderHole(Collider2D collision, string colliderType)
    {
        addHoleParam(collision);

        switch (colliderType)
        {
            case HoleDictionary.OUTER_COLLIDER:
                EventBroadcaster.Instance.PostEvent(EventKeys.OUTER_ENTER_HOLE, holeParams);
                break;
            case HoleDictionary.INNER_COLLIDER:
                EventBroadcaster.Instance.PostEvent(EventKeys.INNER_ENTER_HOLE, holeParams);
                break;
        }
    }
    #endregion

    #region Stay Collider Methods ======================================================
    public void StayColliderProp(Collider2D collision, string colliderType)
    {
        addPropParam(collision);

        switch (colliderType)
        {
            case HoleDictionary.OUTER_COLLIDER:
                EventBroadcaster.Instance.PostEvent(EventKeys.OUTER_STAY_PROP, holeParams);
                break;
            case HoleDictionary.INNER_COLLIDER:
                EventBroadcaster.Instance.PostEvent(EventKeys.INNER_STAY_PROP, holeParams);
                break;
        }
    }
    public void StayColliderHole(Collider2D collision, string colliderType)
    {
        addHoleParam(collision);

        switch (colliderType)
        {
            case HoleDictionary.OUTER_COLLIDER:

                break;
            case HoleDictionary.INNER_COLLIDER:
                EventBroadcaster.Instance.PostEvent(EventKeys.INNER_STAY_HOLE, holeParams);
                break;
        }
    }
    #endregion 

    #region Exit Collider Methods
    public void ExitColliderProp(Collider2D collision, string colliderType)
    {
        addPropParam(collision);

        switch (colliderType)
        {
            case HoleDictionary.OUTER_COLLIDER:
                EventBroadcaster.Instance.PostEvent(EventKeys.OUTER_EXIT_PROP, holeParams);
                break;
            case HoleDictionary.INNER_COLLIDER:
                EventBroadcaster.Instance.PostEvent(EventKeys.INNER_EXIT_PROP, holeParams);
                break;
        }
    }

    public void ExitColliderHole(Collider2D collision, string colliderType)
    {
        addHoleParam(collision);

        switch (colliderType)
        {
            case HoleDictionary.OUTER_COLLIDER:
                break;
            case HoleDictionary.INNER_COLLIDER:
                EventBroadcaster.Instance.PostEvent(EventKeys.INNER_EXIT_HOLE, holeParams);
                break;
        }
    }
    #endregion

    #region Poolable Functions  
    public override void OnInstantiate()
    {
       // InitializeProp();
    }

    public override void OnActivate()
    {
        /*
        _child_prop.transform.localPosition = Vector3.zero;
        this.transform.localRotation = Quaternion.identity;*/
    }

    public override void OnDeactivate()
    {
    }
    #endregion
}
