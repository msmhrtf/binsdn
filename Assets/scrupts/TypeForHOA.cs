using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TypeForHOA : MonoBehaviour {

    public bool hoa_g_big;
    public bool hoa_g_small;
    public bool hoa_p_small;
    public bool hoa_p_big;

    // function that simply returns selected rendering type as a string
    public string getTypeHoa()
    {
        if (hoa_g_big)
            return "hoa_g_big";
        else if (hoa_g_small)
            return "hoa_g_small";
        else if (hoa_p_big)
            return "hoa_p_big";
        else if (hoa_p_small)
            return "hoa_p_small";

        else return "";
    }

}


