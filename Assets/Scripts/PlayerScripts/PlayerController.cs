using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour, IMovableKB, IMovableM
{
    [SerializeField] private GameObject _player_game_object;
    [SerializeField] private Collider2D _game_bounds_collider;
    [SerializeField] private Bounds _game_bounds;

    Vector3 tempVec;
    void Start()
    {
        if (_player_game_object == null)
            _player_game_object = this.transform.parent.gameObject;

        _game_bounds = _game_bounds_collider.bounds;
    }

    #region IMovableKB
    public void MoveKB(Vector2 inputs, float moveSpeed)
    {
        tempVec = new Vector3(_player_game_object.transform.position.x + (inputs.x * Time.deltaTime * moveSpeed), _player_game_object.transform.position.y + (inputs.y * Time.deltaTime * moveSpeed));

        if (isWithinBounds(tempVec))
        {
            _player_game_object.transform.position = tempVec;
        }
    }
    #endregion

    #region IMovableM
    public void MoveM(Vector2 pointerLocation, float moveSpeed)
    {
        tempVec = getDirection(pointerLocation);
        tempVec = new Vector3(
                    _player_game_object.transform.position.x + (tempVec.x * Time.deltaTime * moveSpeed),
                    _player_game_object.transform.position.y + (tempVec.y * Time.deltaTime * moveSpeed));
        if (isWithinBounds(tempVec))
        {
            _player_game_object.transform.position = tempVec;
        }
    }
    private bool isWithinBounds(Vector3 targetLocation)
    {
        if (_game_bounds.Contains(tempVec))
        {
            return true;
        }
        return false;
    }
    #endregion
    private Vector3 getDirection(Vector2 pointerLocation)
    {
        return Vector3.Normalize(new Vector2(pointerLocation.x- _player_game_object.transform.localPosition.x, pointerLocation.y - _player_game_object.transform.localPosition.y));
    }


}
