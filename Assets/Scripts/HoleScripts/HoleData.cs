using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HoleData : MonoBehaviour
{
    #region Hole Data Values

    [SerializeField] private int _hole_level;
    public int HoleLevel
    {
        get { return _hole_level; }
        set { _hole_level = value; }
    }

    [SerializeField] private int _hole_size;
    public int HoleSize
    {
        get { return _hole_size; }
        set { _hole_size = value; }
    }

    [SerializeField] private int _hole_experience;
    public int HoleExperience
    {
        get { return _hole_experience; }
        set { _hole_experience = value; }
    }

    [SerializeField] private int _current_exp_threshold;
    public int HoleCurrentExpThreshold
    {
        get { return _current_exp_threshold; }
        set { _current_exp_threshold = value; }
    }

    [SerializeField] private Color _hole_color;
    #endregion


    private bool lvledUp = false;

    #region Hole Data Methods
    public void InitializeHoleData(int level, Color color, int baseThreshold, int rivalMultiplier)
    {
        _hole_color = color;
        _hole_level = level;
        _hole_experience = (level - 1) * baseThreshold * rivalMultiplier;
    }


    public bool AddHoleExp(int exp, int baseThreshold, float thresholdMultiplier)
    {
        lvledUp = false;

        _hole_experience += exp;

        while (_hole_experience >= _current_exp_threshold)
        {
            _hole_level++;
            _current_exp_threshold += baseThreshold + (baseThreshold * (int)Math.Round((_hole_level - 1) * thresholdMultiplier));
            lvledUp = true;
        } 

        return lvledUp;
    }

    #endregion
}
