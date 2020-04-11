using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FourthOrderFilter {

    // coefficients
    private double a0;
    private double a1;
    private double a2;
    private double a3;
    private double a4;
    private double a5;
    private double a6;
    private double a7;
    private double a8;

    // state
    private float x1;
    private float x2;
    private float x3;
    private float x4;
    private float y1;
    private float y2;
    private float y3;
    private float y4;

    // Constructor
    private FourthOrderFilter()
    {
        // zero initial samples
        x1 = x2 = x3 = x4 = 0;
        y1 = y2 = y3 = y4 = 0;
    }

    // Constructor
    public FourthOrderFilter(double a0, double a1, double a2, double a3, double a4, double b0, double b1, double b2, double b3, double b4)
    {
        SetCoefficients(a0, a1, a2, a3, a4, b0, b1, b2, b3, b4);

        // zero initial samples
        x1 = x2 = x3 = x4 = 0;
        y1 = y2 = y3 = y4 = 0;
    }

    public float Transform(float inSample)
    {
        // compute result
        var result = a0 * inSample + a1 * x1 + a2 * x2 + a3 * x3 + a4 * x4 - a5 * y1 - a6 * y2 - a7 * y3 - a8 * y4;

        // shift samples
        x4 = x3;
        x3 = x2;
        x2 = x1;
        x1 = inSample;

        // shift samples, result to y1
        y4 = y3;
        y3 = y2;
        y2 = y1;
        y1 = (float)result;

        return y1;
    }

    public void SetCoefficients(double aa0, double aa1, double aa2, double aa3, double aa4, double b0, double b1, double b2, double b3, double b4)
    {
        // precompute the coefficients
        a0 = b0 / aa0;
        a1 = b1 / aa0;
        a2 = b2 / aa0;
        a3 = b3 / aa0;
        a4 = b4 / aa0;
        a5 = aa1 / aa0;
        a6 = aa2 / aa0;
        a7 = aa3 / aa0;
        a8 = aa4 / aa0;
    }

    public void clear()
    {
        x1 = x2 = x3 = x4 = 0;
        y1 = y2 = y3 = y4 = 0;
    }
}
