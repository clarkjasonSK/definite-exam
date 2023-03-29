using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Prop : Poolable, IPullable, IAbsorbable
{
    [SerializeField] private GameValues _game_values;
    [SerializeField] private Transform _child_prop;
    [SerializeField] private PropData _prop_data;
    
    #region Coroutines
    private IEnumerator _pulling_prop;
    private IEnumerator _pulling_prop_anchor;
    private IEnumerator _absorbing;
    private IEnumerator _stop_absorbing;
    #endregion

    public int PropSize
    {
        get { return _prop_data.PropSize; }
    }
    public int PropPoints
    {
        get { return _prop_data.PropPoints; }
    }

    private bool _is_absorbing;
    public bool IsBeingAbsorbed
    {
        get { return _is_absorbing; }
    }


    #region Cache Variables
    private Vector2 propPosition;
    private float propScale;
    #endregion

    public void OnEnable() // for when not born from an object pool, but placed and Propdata adjusted manually
    {
        if (poolOrigin != null)
            return;

        InitializeProp();

        propScale = _prop_data.PropSize * _game_values.PropsScaleBase * _game_values.PropsScaleMultiplier;

        this.transform.localScale = new Vector3(propScale, propScale, 1);
    }

    private void InitializeProp() // by self
    {
        if (_game_values == null)
            _game_values = GameManager.Instance.GameValues;

        if (_prop_data == null)
            _prop_data = GetComponent<PropData>();
    }
    public void InitializeProp(int propSize) // from pool
    {
        _prop_data.InitializeData(propSize);

        this.transform.localPosition = PropHandler.Instance.PropStaff.PropHelper.getPropSpawnPoint(_prop_data.PropSpawnPoint);

        propScale = _prop_data.PropSize * _game_values.PropsScaleBase * _game_values.PropsScaleMultiplier;

        this.transform.localScale = new Vector3(propScale, propScale, 1);

    }

    #region IPullable
    public void Pull(Transform target)
    {
        if (!this.gameObject.activeInHierarchy || _is_absorbing)
            return;

        if (_pulling_prop is not null)
            StopCoroutine(_pulling_prop);


        propPosition = _child_prop.transform.localPosition == this.transform.localPosition ? this.transform.position : propPosition = _child_prop.transform.position;

        this.transform.localPosition = target.transform.localPosition;
        _child_prop.transform.position = propPosition;

        _pulling_prop = Pulling();
        _pulling_prop_anchor = PullingAnchor(target);
        StartCoroutine(_pulling_prop);
        StartCoroutine(_pulling_prop_anchor);
        
    }
    public IEnumerator Pulling()
    {
        float current=0;

        while (_child_prop.transform.localPosition.x != 0 && _child_prop.transform.localPosition.y != 0)
        {

            current = Mathf.MoveTowards(current, 1, _game_values.HolePullStrengthProp * Time.deltaTime);

            // prop pulling translation
            _child_prop.transform.localPosition = Vector2.Lerp(_child_prop.transform.localPosition, Vector2.zero, current);

            yield return null;
        }

        yield break;
    }
    public IEnumerator PullingAnchor(Transform target)
    {

        while (this.transform.localPosition.x != target.transform.localPosition.x*_game_values.PropAnchorAim && this.transform.localPosition.y != target.transform.localPosition.y * _game_values.PropAnchorAim)
        {
            // swirl rotation
            this.transform.Rotate(Vector3.back, _game_values.HoleWhirlStrength * Time.deltaTime);

            this.transform.localPosition = Vector2.Lerp(this.transform.localPosition, target.transform.localPosition, _game_values.HolePullStrengthPropAnchor * Time.deltaTime);

            yield return null;
        }

        yield break;
    }

    public void PullStop()
    {
        StopCoroutine(_pulling_prop_anchor);
    }

    #endregion

    #region IAbsorbable
    public void Absorb(EventParameters param)
    {
        if (!this.gameObject.activeInHierarchy)
            return;

        if(_stop_absorbing is not null)
            StopCoroutine(_stop_absorbing);

        _absorbing = Absorbing(param);
        StartCoroutine(_absorbing);
    }
    public IEnumerator Absorbing(EventParameters param)
    {
        _is_absorbing = true;
        while (this.transform.localScale.x > _game_values.PropScaleDespawn)
        {
            this.transform.localScale = Vector2.Lerp(this.transform.localScale, Vector2.zero, _game_values.HoleAbsorbStrengthProp*Time.deltaTime);

            yield return null;
        }

        // TEMPORARY, EVENTUALLY FIND A WAY TO NOT HAVE TO GET PARAMS FROM HANDLER
        param.AddParameter(EventParamKeys.PROP_PARAM, this);
        EventBroadcaster.Instance.PostEvent(EventKeys.PROP_ABSORBED, param);
        _absorbing = null;
        _is_absorbing = false;
        yield break;
    }

    public void AbsorbStop()
    {
        if (!this.gameObject.activeInHierarchy || _absorbing == null)
            return;
        

        if(_absorbing is not null)
            StopCoroutine(_absorbing);

        _stop_absorbing = StopAbsorbing();
        StartCoroutine(_stop_absorbing);
    }
    public IEnumerator StopAbsorbing()
    {
        while (this.transform.localScale.x < propScale)
        {
            this.transform.localScale = Vector2.Lerp(this.transform.localScale, new Vector2(propScale, propScale), _game_values.HoleAbsorbStrengthProp * Time.deltaTime);
            yield return null;
        }

        _stop_absorbing = null;
        yield break;
    }

    #endregion

    #region Poolable Functions  
    public override void OnInstantiate()
    {
        InitializeProp();
    }

    public override void OnActivate()
    {
        _child_prop.transform.localPosition = Vector3.zero;
        this.transform.localRotation = Quaternion.identity;
    }

    public override void OnDeactivate()
    {
        _is_absorbing = false;
    }
    #endregion
}
