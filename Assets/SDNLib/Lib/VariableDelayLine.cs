using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System;
using AForge.Math;

public class VariableDelayLine{
    float[] delayBuffer; //il delay

    public VariableDelayLine(){
        clearDelay();
    }
    public void clearDelay(){
        delayBuffer = new float[0];
    }

    public float[] processDelay(float[] input, int newDelay){

        //La buffersize la becco direttamente dall'input
        float[] outputBuffer = new float[input.Length];
        int bufferSize = input.Length;

        float numerator = bufferSize - 1;
        float denominator = bufferSize - 1 + newDelay - delayBuffer.Length;
        float compressionFactor = numerator / denominator;

        //Add samples from buffer at the start of the outputBuffer

        
        for(int i = 0; i < delayBuffer.Length; i++){
            outputBuffer[i] = delayBuffer[i];
        }

        //Add samples from input until end of the outputBuffer
        //check if old delay and new delay are the same
        if(newDelay == delayBuffer.Length){
            int j = 0;
            //Fill the buffer with the new samples
            for(int i = delayBuffer.Length; i < outputBuffer.Length; i++){
                outputBuffer[i] = input[j];
                j++;
            }
            //Add remaining sample to delayBuffer
            for(int i = 0; i < delayBuffer.Length; i++){
                delayBuffer[i] = input[j];
                j++;
            }
        }
        else{   //else apply the compression/expansion algorithm
            int j;
            float rest;
            //last sample must be treated in different way
            int forLoopEnd;
            if(newDelay==0){ forLoopEnd = input.Length -1;}else{
                forLoopEnd = input.Length;
            }
            float position = 0;

            for(int i = delayBuffer.Length; i < forLoopEnd; i++){
                j = (int)position;
                rest = position - j;
                outputBuffer[i] = (1.0f-rest)*input[j] + (0.0f+rest)*input[j+1];
                position += compressionFactor;
            }

            //The last loop iteration must be addressed in a special way if newDelay = 0 (part 2)
            if(newDelay==0){
                outputBuffer[outputBuffer.Length-1] = input[input.Length-1];
                clearDelay();
            }
            else{
                //int tempdelBuffLength = delayBuffer.Length;
                delayBuffer = new float[newDelay];
                for(int i = 0; i < newDelay - 1; i++){
                    j = (int)position;
                    rest = position - j;
                    //Debug.Log("Delay: " + newDelay + " i: " + i + " j: " + j + " rest: " + rest + " CompressionFactor: " + compressionFactor + " position: " + position + "oldDelBuff" + tempdelBuffLength);
                    float a = (1.0f-rest)*input[j];
                    float b = (0.0f+rest)*input[j+1];
                    //delayBuffer[i] = (1-rest)*input[j] + rest*input[j+1];
                    delayBuffer[i] = a + b;
                    position += compressionFactor;
                }
                delayBuffer[delayBuffer.Length-1] = input[input.Length-1];
            }
        }

        return outputBuffer;
    }
}

public class StereoVariableDelayLine{
    VariableDelayLine[] vdl;
    public StereoVariableDelayLine(){
        vdl = new VariableDelayLine[2];
        vdl[0] = new VariableDelayLine();
        vdl[1] = new VariableDelayLine();
    }

    public float[][] processDelay(float[][] input, int leftDelay, int rightDelay){
        float[][] output = new float[2][];
        output[0] = vdl[0].processDelay(input[0], leftDelay);
        output[1] = vdl[1].processDelay(input[1], rightDelay);
        return output;
    }
}