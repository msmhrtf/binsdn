using UnityEngine;
using System.Collections.Generic;


/*
                case "rockfon": // rockfon panel
                    return filt = new FourthOrderFilter();
                case "priviwood":
                    return filt = new FourthOrderFilter(0.6876, -1.9207, 1.7899, -0.5567, 0, 1, -2.7618, 2.5368, -0.7749, 0);
                case "butterworthLow":
                    return filt = new FourthOrderFilter(1.0000, 0.7821, 0.6800, 0.1827, 0.0301, 0.1672, 0.6687, 1.0031, 0.6687, 0.1672);
                case "allpass":
                    return filt = new FourthOrderFilter(1.0000, 3.9978, 5.9934, 3.9934, 0.9978,0.9989, 3.9956, 5.9934, 3.9956, 0.9989);
                default:
                    System.Console.WriteLine("Wall " + wallName + ": Non existing wall material.");
                    return filt = null;
 */

public class AudioMaterials
{
    struct AudioMat{
        public string name;
        public FourthOrderFilter filter;
        public AudioMat(string name, FourthOrderFilter filter) {
            this.name = name;
            this.filter = filter;
        }
    }

    static AudioMat[] materialCollection = new AudioMat[] {
        new AudioMat("concrete", new FourthOrderFilter(1, -3.52116135875940, 4.71769322757540, -2.85612549976053, 0.659726857770080, 0.975675045601632, -3.43354999748689, 4.59790176394788, -2.78225880232183, 0.642363879989748)),
        new AudioMat("carpet", new FourthOrderFilter(1, -1.08262757037834, -0.409156873201483, 0.393177044223513, 0.115374144111459, 0.557133737058345, -0.595471680050212, -0.198869264528946, 0.202098010808594, 0.0519704029964675)),
        new AudioMat("glass", new FourthOrderFilter(1, -3.88157817484550, 5.67768298901651, -3.70995794379865, 0.913857561160056, 0.988950446602402, -3.84068106116436, 5.62070430601924, -3.67455649442745, 0.905586752101161)),
        new AudioMat("gypsum", new FourthOrderFilter(1, -3.56627223138536, 4.86547502358377, -3.01433043560409, 0.716117186547968, 0.489588360222364, -1.76575810943288, 2.43564604718648, -1.52441182610865, 0.365712142392197)),
        new AudioMat("vinyl", new FourthOrderFilter(1, -1.20321523440173, -0.152249555702279, 0.452752314993338, -0.0802737840229035, 0.951113234620835, -1.14521870903135, -0.126014399528383, 0.412593832519338, -0.0756816378410196)),
        new AudioMat("wood", new FourthOrderFilter(1, -3.80539017238370, 5.45234860126098, -3.48668369576338, 0.839765796280106, 0.949092279345441, -3.61085651980882, 5.17248568134037, -3.30701292959816, 0.796328272495264)),
        new AudioMat("rockfon", new FourthOrderFilter(1, -1.10082609915788, -0.197118900834112, 0.396353649853209, -0.0863017873494277, 0.604052413410784, -0.840106608467757, -0.0638796553407742, 0.478513769747507, -0.165417289996871))
    };
    
    public static FourthOrderFilter getMaterial(string name) {
        foreach (AudioMat el in materialCollection) {
            if (el.name.Equals(name)) {
                return el.filter;
            }
        }
        System.Console.WriteLine("Wall " + name + ": Non existing wall material.");
        return null;
    }

    public static string[] getMaterialList() {
        string[] tmp = new string[materialCollection.Length];
        for (int i = 0; i < materialCollection.Length; i++) {
            tmp[i] = materialCollection[i].name;
        }
        return tmp;
    }

}

