using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    #region System Values ScriptableO's
    [SerializeField] private GameValues _game_values;
    [SerializeField] private VisualValues _visual_values;
    #endregion

    #region Player Variables

    [SerializeField] private PlayerScriptableObject _player_so;
    [SerializeField] private PlayerController _player_controller;
    public int PlayerID
    {
        get { return _player_so.PlayerID; }
    }
    public string PlayerName
    {
        get { return _player_so.PlayerName; }
    }
    public int PlayerHoleLevel
    {
        get { return _hole.HoleLevel; }
    }
    public int PlayerScore
    {
        get { return _player_so.PlayerScore; }
    }
    public int PlayerNextLvl
    {
        get { return _player_so.PlayerNextLvl; }
    }
    #endregion

    [SerializeField] private Hole _hole;
    [SerializeField] private TextMesh _text_mesh;

    #region Cache Variables
    private float speedMultiplier;
    #endregion

    #region Event Paramaters
    private EventParameters playerParam;
    #endregion


    public void InitializePlayer(PlayerScriptableObject playerSo)
    {
        this._player_so = playerSo;
    }
    private void Start()
    {
        if (_game_values == null)
            _game_values = GameManager.Instance.GameValues;

        if (_visual_values == null)
            _visual_values = GameManager.Instance.VisualValues;

        if (_hole == null)
            _hole = GetComponentInParent<Hole>();

        if (_player_controller == null)
            _player_controller = GetComponent<PlayerController>();

        if (_text_mesh == null)
            _text_mesh = GetComponent<TextMesh>();

        playerParam = new EventParameters();


        _text_mesh.fontSize = _visual_values.NameTagBaseFontSize;

        SetTextMesh("" + _player_so.PlayerName + " | Lvl " + _hole.HoleLevel);

        speedMultiplier = _game_values.PlayerSpeedDecreaseMultiplier;
        UpdateScores(0, _game_values.HoleExpBaseThreshold);
        CameraHandler.Instance.SetCamFollowTarget(this.transform.parent.transform);
    }
    private void Update()
    {
        
        if (_player_so.PlayerID==2 && InputHandler.Instance.UserKeyHold)
        {
            movePlayerKeyboard(speedMultiplier);

            return; 
        }

        if (_player_so.PlayerID == 1)
        {
            movePlayerMouse(speedMultiplier);
            return;
        }
    } 

    public void SetTextMesh(string text)
    {
        _text_mesh.text = text;
    }

    public void UpdateScores(int score, int nxtLvl)
    {
        _player_so.SetScores(score, nxtLvl);
        playerParam.AddParameter(EventParamKeys.PLAYER_PARAM, this);

        EventBroadcaster.Instance.PostEvent(EventKeys.PLAYER_SCORE_UPDATE, playerParam);
    }

    public void UpdateLevelScale()
    {
        if (this.transform.localScale.x > _visual_values.NameTagFloor)
        {
            scaleDownPlayerSize();
        }
        SetTextMesh("" + _player_so.PlayerName + " | Lvl " + _hole.HoleLevel);

        if( speedMultiplier > _game_values.PlayerSpeedDecreaseFloor)
            speedMultiplier *= _game_values.PlayerSpeedDecreaseMultiplier;
    }


    private void scaleDownPlayerSize()
    {
        this.transform.localScale = new Vector3(this.transform.localScale.x * _visual_values.NameTagBalancing, this.transform.localScale.y * _visual_values.NameTagBalancing, 1);
    }

    private void movePlayerKeyboard(float decreaseSpeed)
    {
        _player_controller.MoveKB(InputHandler.Instance.UserKeyInput, _game_values.PlayerBaseSpeed * decreaseSpeed );
    }
    private void movePlayerMouse(float decreaseSpeed)
    {
        // just a big block of text making sure it doesn't jitter at cursor point
        if( ((InputHandler.Instance.UserCursorInput.x - _game_values.PlayerCursorOffset) < this.transform.parent.transform.localPosition.x && this.transform.parent.transform.localPosition.x < (InputHandler.Instance.UserCursorInput.x + _game_values.PlayerCursorOffset)) && (((InputHandler.Instance.UserCursorInput.y - _game_values.PlayerCursorOffset) < this.transform.parent.transform.localPosition.y) &&(this.transform.parent.transform.localPosition.y < (InputHandler.Instance.UserCursorInput.y + _game_values.PlayerCursorOffset))))
            return;

        _player_controller.MoveM(InputHandler.Instance.UserCursorInput, _game_values.PlayerBaseSpeed * decreaseSpeed);
    }


}
