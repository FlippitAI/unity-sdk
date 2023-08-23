using OpenAI;
using System;
using UnityEngine;

namespace Flippit
{
    public class EnumLists 
    {
       
        public enum Personality {
            Extroverted/*"b0f9fb6a-3080-4f1e-81f4-582d7d684afc"*/,
            Agreeable/*"b0f9fb6a-3080-4f1e-81f4-582d7d684afc"*/,
            Optimistic/*"dab7828f-0883-44b9-b032-9e7b2797cf7a"*/,
            Empathetic/*"1b5e78ea-6205-46a9-b904-3dac39480554"*/,
            Adventurous/*"3c4c4714-71f1-4ac9-acb8-123900018295"*/,
            Introverted/*"8edd1259-863f-4e09-8fdf-f3d66e355f47"*/,
            Disagreeable/*"5f61cae1-3be2-4ad9-8441-6da68902150b"*/,
            Pessimistic/*"948458d2-b732-4674-8e84-b07c9aac14b7"*/,
            Coldhearted/*"6d94ab90-9776-41dd-b172-6dc7e30e4390"*/,
            Shy/*"91dd96bf-737f-4670-9974-082339e66d6a"*/
        };
        public string[] personalitiesID = new string[] 
        {
            "b0f9fb6a-3080-4f1e-81f4-582d7d684afc",
            "90067680-fa75-4cee-a65a-a5c4011d16ad",
            "dab7828f-0883-44b9-b032-9e7b2797cf7a",
            "1b5e78ea-6205-46a9-b904-3dac39480554",
            "3c4c4714-71f1-4ac9-acb8-123900018295",
            "8edd1259-863f-4e09-8fdf-f3d66e355f47",
            "5f61cae1-3be2-4ad9-8441-6da68902150b",
            "948458d2-b732-4674-8e84-b07c9aac14b7",
            "6d94ab90-9776-41dd-b172-6dc7e30e4390",
            "91dd96bf-737f-4670-9974-082339e66d6a"
        };
        public enum Voices {
            None/*"ec03c292-fb7a-4cde-8743-98f77a38a357"*/,
            MaleEnglishUK/*"0deaad6b-9bad-42e5-9813-02f1c8b5d5e0"*/,
            FemaleEnglishUK/*"8680c8e5-6245-47db-a183-d554ad19dc27"*/,
            MaleEnglishUS/*"f0d45b31-de56-436d-9be7-7229189ee086"*/,
            FemaleEnglishUS/*"69cdaeef-4b5e-4890-8a8a-ce8e801ccd7e"*/,
            FemaleEnglishAutralia/*"cc2e9179-7bad-4e6c-9412-c64a3aec4be3"*/,
            FemaleEnglishNewZealand/*"ca4c2983-5239-4f91-b3f1-f1751114d8d5"*/,
            FemaleEnglishIndia/*"3688bbb7-8c62-49fe-98e9-5084e996c9d2"*/,
            FemaleEnglishIrish/*"61562b04-f758-4dc3-90d3-f6bf7716b232"*/,
            FemaleEnglishSouthAfrica/*"72e6feb2-d073-4725-b32e-1dbf7f97e70b"*/,
            MaleFrench/*"dad858a1-2f6c-4b26-85f0-63c6a679a359"*/,
            FemaleFrench/*"a3115d73-631d-4c86-bf96-8a541db44d79"*/,
            MaleItalian/*"880fb304-b419-4a21-a8d6-07f7eb2a904b"*/,
            FemaleItalian/*"ca6bb664-3880-4fb3-b463-2caa242a9a1f"*/
        };
        public string[] VoicesID = new string[]
        {
            "ec03c292-fb7a-4cde-8743-98f77a38a357",/*no_voice*/
            "0deaad6b-9bad-42e5-9813-02f1c8b5d5e0",/*Arthur*/
            "8680c8e5-6245-47db-a183-d554ad19dc27",/*Emma*/
            "f0d45b31-de56-436d-9be7-7229189ee086",/*Matthew*/
            "69cdaeef-4b5e-4890-8a8a-ce8e801ccd7e",/*Kimberly*/
            "cc2e9179-7bad-4e6c-9412-c64a3aec4be3",/*Olivia*/
            "ca4c2983-5239-4f91-b3f1-f1751114d8d5",/*Aria*/
            "3688bbb7-8c62-49fe-98e9-5084e996c9d2",/*Kajal*/
            "61562b04-f758-4dc3-90d3-f6bf7716b232",/*Niamh*/
            "72e6feb2-d073-4725-b32e-1dbf7f97e70b",/*Ayanda*/
            "dad858a1-2f6c-4b26-85f0-63c6a679a359",/*Remi*/
            "a3115d73-631d-4c86-bf96-8a541db44d79",/*Lea*/
            "880fb304-b419-4a21-a8d6-07f7eb2a904b",/*Adriano*/
            "ca6bb664-3880-4fb3-b463-2caa242a9a1f"/*Bianca*/
        };
        public string[] voiceNames = new string[]
        {
           "no_voice",
            "Arthur",
            "Emma",
            "Matthew",
            "Kimberly",
            "Olivia",
            "Aria",
            "Kajal",
            "Niamh",
            "Ayanda",
            "Remi",
            "Lea",
            "Adriano",
            "Bianca",
        };
        public enum Age 
        {
            YoungAdult/* "3fa85f64-5717-4562-b3fc-2c963f66afa6"*/,
            Child/*"a0f8d39b-c41e-4e05-9462-83bc36444633"*/,
            MiddleAged/*"88017722-dfe9-4078-a864-449a05984eb0"*/,
            Old/*"ae89e949-d2be-4c72-9cc4-18d30139c867"*/
        };
        public string[] AgeID = new string[] 
        { 
            "3fa85f64-5717-4562-b3fc-2c963f66afa6",
            "a0f8d39b-c41e-4e05-9462-83bc36444633",
            "88017722-dfe9-4078-a864-449a05984eb0",
            "ae89e949-d2be-4c72-9cc4-18d30139c867"
        };
        public enum Animation 
        {
            Talking/**/,
            Afraid/**/,
            Angry/**/,
            Surprised/**/,
            Excited/**/,
            Bragging/**/,
            Greetings/**/,
            Jogging/**/,
            Levitating/**/,
            Singing/**/,
            Searching/**/,
            Magic/**/
        };
        public string[] AnimationsID = new string[] 
        {
        
        };

    }
}
