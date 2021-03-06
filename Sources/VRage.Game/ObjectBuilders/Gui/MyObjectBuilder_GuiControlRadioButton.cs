﻿using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    public enum MyGuiControlRadioButtonStyleEnum
    {
        FilterCharacter,
        FilterGrid,
        FilterAll,
        FilterEnergy,
        FilterStorage,
        FilterSystem,
        ScenarioButton,
        Rectangular
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GuiControlRadioButton : MyObjectBuilder_GuiControlBase
    {
        [ProtoMember]
        public int Key;

        [ProtoMember]
        public MyGuiControlRadioButtonStyleEnum VisualStyle;
    }
}