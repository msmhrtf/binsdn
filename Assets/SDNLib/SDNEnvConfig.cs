using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.IO;
using System.Linq;



public struct HRTFData
{
    public AForge.Math.Complex[][] HRTFs;
    public float[] Delays;
}
public class SDNEnvConfig : MonoBehaviour
{
    public string CIPIC;
    public bool UsePersonalizedSDN = true;
    public bool UsePersistentDataPath = false;
    //public HRTFcontainer HRTFCamera;
    public bool UseNewestSubject = false;






    ///<summary>
    ///OLD HRTFcontainer
    /// </summary>
    ///



    private string cipic_subject;

    private static int hrtfLength = 200;
    private static int azNum = 27;
    private static int elNum = 52;

    private bool loaded = false; // flag for hrtf loading finish
    private int buffSize;
    private int fftLength;

    // Triple Jagged Arrays containing all azimuth and elevations hrtfs. Matrix of (25,50,200)
    private AForge.Math.Complex[][][] matrix_l = new AForge.Math.Complex[azNum][][]; // left hrtf 3d matrix, as in Matlab
    private AForge.Math.Complex[][][] matrix_r = new AForge.Math.Complex[azNum][][]; // right hrtf 3d matrix, as in Matlab

    // Delaunay triangles and points (in Matlab)
    private int[][] triangles = new int[2652][]; // 2652 x 3-> punto1, punto2, punto3 datio in base alla posizione su points
    private float[][] points = new float[1404][]; // 1404 x 2-> azimuth e elevation?

    //private int[][] itds = new int[1404][]; // 1404 righe x 2-> itd left e right
    private float[] headsize = new float[2];

    // interp param
    float g1, g2, g3, det;
    float[] A, B, C, T, invT, X;
    int[][] indices = new int[3][];

    private float[] azimuths = new float[27] { -90, -80, -65, -55, -45, -40, -35, -30, -25, -20, -15, -10, -5, 0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 55, 65, 80, 90 };
    private float[] elevations = new float[52] { -90, -45, -39.374f, -33.748f, -28.122f, -22.496f, -16.87f, -11.244f, -5.618f, 0.00800000000000267f, 5.634f, 11.26f, 16.886f, 22.512f, 28.138f, 33.764f, 39.39f, 45.016f, 50.642f, 56.268f, 61.894f, 67.52f, 73.146f, 78.772f, 84.398f, 90.024f, 95.65f, 101.276f, 106.902f, 112.528f, 118.154f, 123.78f, 129.406f, 135.032f, 140.658f, 146.284f, 151.91f, 157.536f, 163.162f, 168.788f, 174.414f, 180.04f, 185.666f, 191.292f, 196.918f, 202.544f, 208.17f, 213.796f, 219.422f, 225.048f, 230.674f, 270 };

    public int BufferSize = 1024;
    public int SystemSampleRate = 44100;










    private void Awake()
    {

    
        string folderdelimit = "/";
        #if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
                         folderdelimit = "\\";
        #endif

        if(UsePersistentDataPath){
            if(!UseNewestSubject)
            Debug.Log("Using your Persistend Data Path-->" + Application.persistentDataPath);
        }

        //if use persistentdatapath get newest directory inside and print it
        if(UsePersistentDataPath && UseNewestSubject){
            string[] dirs = Directory.GetDirectories(Application.persistentDataPath);
            string newestDir = dirs[0];
            foreach(string dir in dirs){
                if(Directory.GetCreationTime(dir) > Directory.GetCreationTime(newestDir)){
                    newestDir = dir;
                }
            }
            
            //take the last folder name
            String last = newestDir.Split(folderdelimit).Last();
            //remove the last 4 char from the string and the first 7
            last = last.Substring(7, last.Length-11);
            Debug.Log("Using NewestSubject in Persistend Data Path-->" + last);
            CIPIC = last;
        }


        AudioConfiguration AC = AudioSettings.GetConfiguration();
        AC.dspBufferSize = BufferSize;
        AC.sampleRate = SystemSampleRate;
        AudioSettings.Reset(AC);

        //CREO IL BOUNDARY
        GameObject bound = new GameObject("bounds_DO_NOT_REMOVE");
        bound.AddComponent<boundary>();        
        
        buffSize = AC.dspBufferSize;

        fftLength = buffSize;
        int lats = buffSize*1000/AC.sampleRate;
        Debug.Log("SDN Lib correctly started at " + AC.sampleRate + "Hz with sample buffer " + AC.dspBufferSize+". Latency is " + lats +"ms");

        SwitchHRTF(CIPIC);
        /*
        for (int i = 0; i < matrix_l.Length; i++)
        {
            matrix_l[i] = new AForge.Math.Complex[elNum][];
            matrix_r[i] = new AForge.Math.Complex[elNum][];
            for (int jj = 0; jj < matrix_l[i].Length; jj++)
            {
                matrix_l[i][jj] = new AForge.Math.Complex[fftLength];
                matrix_r[i][jj] = new AForge.Math.Complex[fftLength];
            }
        }

        // HRTF selection -> generic or personalised

        if (UsePersonalizedSDN)
        {
            cipic_subject = CIPIC;
            if (cipic_subject == "165")
                Debug.Log("NEED A PERSONALISED HRTF DATASET ! CHANGE IN SUBJECT_INFO.");
        }
        else
            cipic_subject = "165"; // KEMAR HATS with small pinnae


        string textFile; // 2d text file containing left and right hrtfs for a specific azimuth
        string fileName;

        TextAsset[] txt_a;


        if (!UsePersistentDataPath)
        {
            txt_a = Resources.LoadAll<TextAsset>("subject" + cipic_subject + "_txt");
        }
        else {
            Debug.Log("Using your Persistend Data Path-->" + Application.persistentDataPath + folderdelimit + "subject" + cipic_subject + "_txt");
            string[] files = Directory.GetFiles(Application.persistentDataPath + folderdelimit + "subject" + cipic_subject + "_txt", "*.txt");
            txt_a = new TextAsset[files.Length];
            for (int i = 0; i < files.Length; i++) {
                txt_a[i] = new TextAsset(File.ReadAllText(files[i]));
                txt_a[i].name = files[i].Split(folderdelimit).Last();
            }
        }

        //LOAD HRTF IMPULSES AND DO FFT
        int i_l = 0, i_r = 0, k = 0;      //j_l = 0, j_r = 0, k_l = 0, k_r = 0;
        int j = 0;

        foreach (TextAsset txtfile in txt_a)
        {

            fileName = txtfile.name;
            textFile = txtfile.text;

            j = 0;
            k = 0;

            int itds_id_l = 0;
            int itds_id_r = 0;


            if (fileName[0].Equals('L') == true)
            {
                foreach (var row in textFile.Split('\n'))
                {
                    if (j == elNum)
                    {
                        break;
                    }
                    k = 0;

                    foreach (var col in row.Trim().Split(' '))
                    {
                        matrix_l[i_l][j][k] = new AForge.Math.Complex(float.Parse(col.Trim(), System.Globalization.NumberStyles.Float), 0);
                        k++;
                    }
                    //matrix_l[i_l][j] = getMovedHRTF(matrix_l[i_l][j],itds_id_l,0);
                    itds_id_l++;
                        
                    // compute fft
                    AForge.Math.FourierTransform.FFT(matrix_l[i_l][j], AForge.Math.FourierTransform.Direction.Forward);
                    j++;
                }
                i_l++;
            }
            else if (fileName[0].Equals('R') == true)
            {
                foreach (var row in textFile.Split('\n'))
                {
                    if (j == elNum)
                    {
                        break;
                    }
                    k = 0;
                    foreach (var col in row.Trim().Split(' '))
                    {
                        matrix_r[i_r][j][k] = new AForge.Math.Complex(float.Parse(col.Trim(), System.Globalization.NumberStyles.Float), 0);
                        k++;
                    }
                    //matrix_r[i_r][j] = getMovedHRTF(matrix_r[i_r][j], itds_id_r, 1);
                    itds_id_r++;
                    // compute fft
                    AForge.Math.FourierTransform.FFT(matrix_r[i_r][j], AForge.Math.FourierTransform.Direction.Forward);
                    j++;
                }
                i_r++;
            }
            else if(fileName[0].Equals('h') == true)
            {
                //
                //Load Head Dimensions
                //

                //Insert head size into the array
                string[] size = txtfile.text.Split(' ');
                headsize[0] = (int)float.Parse(size[0].Trim(), System.Globalization.NumberStyles.Float); //X1
                headsize[1] = (int)float.Parse(size[1].Trim(), System.Globalization.NumberStyles.Float); //X3
                Debug.Log("Head size is " + headsize[0] + "x" + headsize[1] + "cm");
                //Debug.Log("WARNING! This file is not in the correct format!");
                //break;
            }else if(fileName[0].Equals('t') == true){
                // LOAD triangles and points
                for (int i = 0; i < triangles.Length; i++)
                {
                    triangles[i] = new int[3];
                }
                

                
                j = 0;
                foreach (var row in txtfile.text.Split('\n'))
                {
                    if (j == 2652)
                    {
                        break;
                    }
                    k = 0;
                    foreach (var col in row.Trim().Split(' '))
                    {
                        triangles[j][k] = int.Parse(col.Trim(), System.Globalization.NumberStyles.Integer);
                        k++;
                    }
                    j++;
                }
            }else if(fileName[0].Equals('p') == true){
                // points
                for (int i = 0; i < points.Length; i++)
                {
                    points[i] = new float[2];
                }

                j = 0;
                foreach (var row in txtfile.text.Split('\n'))
                {
                    if (j == 1404)
                    {
                        break;
                    }
                    k = 0;
                    foreach (var col in row.Trim().Split(' '))
                    {
                        points[j][k] = float.Parse(col.Trim(), System.Globalization.NumberStyles.Float);
                        k++;
                    }
                    j++;
                }
            }else{
                Debug.Log("File " + fileName + " is not in the correct format!");
            }
        
        }

        

        
        

        for (int i = 0; i < indices.Length; i++)
        {
            indices[i] = new int[2];
        }
        T = new float[4];
        invT = new float[4];
        X = new float[2];



        // flag
        loaded = true;


        if (!UsePersistentDataPath)
        {
            Debug.Log("HRTF dataset loaded! CIPIC subject ID: " + cipic_subject);
        }
        else {
            Debug.Log("External HRTF dataset correctly loaded!");
        }
*/
    }















    public AForge.Math.Complex[] getHRTF_left(int index1, int index2)
    {
        return matrix_l[index1][index2];
    }

    public AForge.Math.Complex[] getHRTF_right(int index1, int index2)
    {
        return matrix_r[index1][index2];
    }

    public HRTFData getInterpolated_HRTFDatas(float[] aziEle){

        // Variables initialisation
        int i, j;
        AForge.Math.Complex[][] interpHrtf = new AForge.Math.Complex[2][];
        for (i = 0; i < interpHrtf.Length; i++)
        {
            interpHrtf[i] = new AForge.Math.Complex[fftLength];
        }

        //Interpolation
        i = triangles.Length - 1;
        T = new float[4];
        invT = new float[4];
        X = new float[2];
        while (i >= 0)
        {
            A = points[triangles[i][0] - 1]; // -1 because Matlab indexing
            B = points[triangles[i][1] - 1];
            C = points[triangles[i][2] - 1];
            T[0] = A[0] - C[0];
            T[1] = A[1] - C[1];
            T[2] = B[0] - C[0];
            T[3] = B[1] - C[1];
            invT[0] = T[3];
            invT[1] = -T[1];
            invT[2] = -T[2];
            invT[3] = T[0];
            det = 1 / (T[0] * T[3] - T[1] * T[2]);
            for (j = 0; j < invT.Length; j++)
                invT[j] *= det;
            X[0] = aziEle[0] - C[0];
            X[1] = aziEle[1] - C[1];
            g1 = invT[0] * X[0] + invT[2] * X[1];
            g2 = invT[1] * X[0] + invT[3] * X[1];
            g3 = 1 - g1 - g2;
            if (g1 >= 0 && g2 >= 0 && g3 >= 0)
            {
                indices[0] = getIndices(A[0], A[1]);
                indices[1] = getIndices(B[0], B[1]);
                indices[2] = getIndices(C[0], C[1]);
                for (j = 0; j < fftLength; j++)
                {
                    interpHrtf[0][j] = g1 * matrix_l[indices[0][0]][indices[0][1]][j] + g2 * matrix_l[indices[1][0]][indices[1][1]][j] + g3 * matrix_l[indices[2][0]][indices[2][1]][j];
                    interpHrtf[1][j] = g1 * matrix_r[indices[0][0]][indices[0][1]][j] + g2 * matrix_r[indices[1][0]][indices[1][1]][j] + g3 * matrix_r[indices[2][0]][indices[2][1]][j];
                }
                break;
            }
            i--;
        }

        HRTFData tmp = new HRTFData();
        tmp.HRTFs = interpHrtf;
        tmp.Delays = getInterpolated_ITDs(aziEle);

        return tmp;
    }

    public float[] getInterpolated_ITDs(float[] aziEle){

        //Computes the ITD for a spherical head
        //theta is the angle between the source and the head axis
        //a is the radius of the head
        //fs is the sampling frequency
        //returns the delay in samples

        //ITD from Duda and Brown
        //in cascade after the head shadow filter
        //here we use vertical-polar coordinate system -> theta [-180; 180]

        //Debug.Log("theta = " + theta);

        //Calculate left ear angle
        float thetaL = aziEle[0] + 90;
        //calculate right ear angle
        float thetaR = aziEle[0] - 90;

        double c = 343;

        //convert theta to radiants
        thetaL = thetaL * (float)Math.PI / 180.0f;
        thetaR = thetaR * (float)Math.PI / 180.0f;

        //optimal head radius
        float a = (0.41f*headsize[0]/2 + 0.22f*headsize[1]/2 + 3.7f) / 100;
        
        double ac = a/c;

        double delayL, delayR;

        if (Mathf.Abs(thetaL) < Mathf.PI/2){
            // adding ac to the result in order to have positive delays and keep the system causal, the ITD stays the same.
            delayL = -ac*Mathf.Cos(thetaL)+ac;
        }else{  //è corretto?
            delayL = ac*(Mathf.Abs(thetaL)-Mathf.PI/2)+ac;
        }

        if (Mathf.Abs(thetaR) < Mathf.PI/2){
            // adding ac to the result in order to have positive delays and keep the system causal, the ITD stays the same.
            delayR = -ac*Mathf.Cos(thetaR)+ac;
        }else{  //è corretto?
            delayR = ac*(Mathf.Abs(thetaR)-Mathf.PI/2)+ac;
        }


        //convert time-delays to samples
        float[] delay_samp = {(float)(delayL*SystemSampleRate), (float)(delayR*SystemSampleRate)};

        return delay_samp;

    }

    

    /*public int[] getInterpolated_ITDs(float[] azEl){
        int[] indexes = getIndices(azEl[0], azEl[1]);
        //Debug.Log("Ids: " + indexes[0] + "--" + indexes[1]);
        //Indexes[0] è il calore sulla X (azimuth ANGLE)
        //Indexes[1] è il valore sulla Y (elevation ANGLE)
        return itds[indexes[0]*52 + indexes[1]];
    }*/

    public int[] getIndices(float azimuth, float elevation) // get the matrix indices relative to azimuth (0) and elevation (1)
    {
        int[] indices = new int[2];

        float[] azimuths2 = new float[azNum];
        float[] elevations2 = new float[elNum];

        for (int i = 0; i < azNum; i++) // Is there a way to do it more efficiently than a for loop? 
        {
            azimuths2[i] = Math.Abs(azimuths[i] - azimuth);
        }
        for (int i = 0; i < elNum; i++)
        {
            elevations2[i] = Math.Abs(elevations[i] - elevation);
        }

        // Find index of minimum
        indices[0] = Enumerable.Range(0, azimuths2.Length).Aggregate((a, b) => (azimuths2[a] < azimuths2[b]) ? a : b);
        indices[1] = Enumerable.Range(0, elevations2.Length).Aggregate((a, b) => (elevations2[a] < elevations2[b]) ? a : b);

        return indices;
    }

    public int getHrtfLength()
    {
        return hrtfLength;
    }

    public bool getLoaded()
    {
        return loaded;
    }


    public void SwitchHRTF(string id){
        for (int i = 0; i < matrix_l.Length; i++)
        {
            matrix_l[i] = new AForge.Math.Complex[elNum][];
            matrix_r[i] = new AForge.Math.Complex[elNum][];
            for (int jj = 0; jj < matrix_l[i].Length; jj++)
            {
                matrix_l[i][jj] = new AForge.Math.Complex[fftLength];
                matrix_r[i][jj] = new AForge.Math.Complex[fftLength];
            }
        }

        cipic_subject = id; // KEMAR HATS with small pinnae

        string textFile; // 2d text file containing left and right hrtfs for a specific azimuth
        string fileName;

        TextAsset[] txt_a;

        string folderdelimit = "/";

        #if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
                                 folderdelimit = "\\";
        #endif

        if (!UsePersistentDataPath)
        {
            txt_a = Resources.LoadAll<TextAsset>("subject" + cipic_subject + "_txt");
        }
        else {
            //Debug.Log("Using your Persistend Data Path-->" + Application.persistentDataPath + folderdelimit + "subject" + cipic_subject + "_txt");
            string[] files = Directory.GetFiles(Application.persistentDataPath + folderdelimit + "subject" + cipic_subject + "_txt", "*.txt");
            txt_a = new TextAsset[files.Length];
            for (int i = 0; i < files.Length; i++) {
                txt_a[i] = new TextAsset(File.ReadAllText(files[i]));
                txt_a[i].name = files[i].Split(folderdelimit).Last();
            }
        }

        //LOAD HRTF IMPULSES AND DO FFT
        int i_l = 0, i_r = 0, k = 0;      //j_l = 0, j_r = 0, k_l = 0, k_r = 0;
        int j = 0;

        foreach (TextAsset txtfile in txt_a)
        {

            fileName = txtfile.name;
            textFile = txtfile.text;

            j = 0;
            k = 0;

            int itds_id_l = 0;
            int itds_id_r = 0;


            if (fileName[0].Equals('L') == true)
            {
                foreach (var row in textFile.Split('\n'))
                {
                    if (j == elNum)
                    {
                        break;
                    }
                    k = 0;

                    foreach (var col in row.Trim().Split(' '))
                    {
                        matrix_l[i_l][j][k] = new AForge.Math.Complex(float.Parse(col.Trim(), System.Globalization.NumberStyles.Float), 0);
                        k++;
                    }
                    //matrix_l[i_l][j] = getMovedHRTF(matrix_l[i_l][j],itds_id_l,0);
                    itds_id_l++;
                        
                    // compute fft
                    AForge.Math.FourierTransform.FFT(matrix_l[i_l][j], AForge.Math.FourierTransform.Direction.Forward);
                    j++;
                }
                i_l++;
            }
            else if (fileName[0].Equals('R') == true)
            {
                foreach (var row in textFile.Split('\n'))
                {
                    if (j == elNum)
                    {
                        break;
                    }
                    k = 0;
                    foreach (var col in row.Trim().Split(' '))
                    {
                        matrix_r[i_r][j][k] = new AForge.Math.Complex(float.Parse(col.Trim(), System.Globalization.NumberStyles.Float), 0);
                        k++;
                    }
                    //matrix_r[i_r][j] = getMovedHRTF(matrix_r[i_r][j], itds_id_r, 1);
                    itds_id_r++;
                    // compute fft
                    AForge.Math.FourierTransform.FFT(matrix_r[i_r][j], AForge.Math.FourierTransform.Direction.Forward);
                    j++;
                }
                i_r++;
            }
            else if(fileName[0].Equals('h') == true)
            {
                //
                //Load Head Dimensions
                //

                //Insert head size into the array
                string[] size = txtfile.text.Split(' ');
                headsize[0] = (int)float.Parse(size[0].Trim(), System.Globalization.NumberStyles.Float); //X1
                headsize[1] = (int)float.Parse(size[1].Trim(), System.Globalization.NumberStyles.Float); //X3
                //Debug.Log("Head size is " + headsize[0] + "x" + headsize[1] + "cm");
                //Debug.Log("WARNING! This file is not in the correct format!");
                //break;
            }else if(fileName[0].Equals('t') == true){
                // LOAD triangles and points
                for (int i = 0; i < triangles.Length; i++)
                {
                    triangles[i] = new int[3];
                }
                

                
                j = 0;
                foreach (var row in txtfile.text.Split('\n'))
                {
                    if (j == 2652)
                    {
                        break;
                    }
                    k = 0;
                    foreach (var col in row.Trim().Split(' '))
                    {
                        triangles[j][k] = int.Parse(col.Trim(), System.Globalization.NumberStyles.Integer);
                        k++;
                    }
                    j++;
                }
            }else if(fileName[0].Equals('p') == true){
                // points
                for (int i = 0; i < points.Length; i++)
                {
                    points[i] = new float[2];
                }

                j = 0;
                foreach (var row in txtfile.text.Split('\n'))
                {
                    if (j == 1404)
                    {
                        break;
                    }
                    k = 0;
                    foreach (var col in row.Trim().Split(' '))
                    {
                        points[j][k] = float.Parse(col.Trim(), System.Globalization.NumberStyles.Float);
                        k++;
                    }
                    j++;
                }
            }else{
                Debug.Log("File " + fileName + " is not in the correct format!");
            }

        }

        

        
        

        for (int i = 0; i < indices.Length; i++)
        {
            indices[i] = new int[2];
        }
        T = new float[4];
        invT = new float[4];
        X = new float[2];



        // flag
        loaded = true;


        if (!UsePersistentDataPath)
        {
            Debug.Log("HRTF dataset loaded! CIPIC subject ID: " + cipic_subject);
        }
        else {
            Debug.Log("External HRTF " + cipic_subject + " dataset correctly loaded!");
        }
        
    }

}
