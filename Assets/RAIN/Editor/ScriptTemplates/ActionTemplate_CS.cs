using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RAIN.Core;
using RAIN.Action;

public class ActionTemplate_CS : Action
{
    public ActionTemplate_CS()
    {
        actionName = "ActionTemplate_CS";
    }

    public override ActionResult Start(Agent agent, float deltaTime)
    {
        return ActionResult.SUCCESS;
    }

    public override ActionResult Execute(Agent agent, float deltaTime)
    {
        return ActionResult.SUCCESS;
    }

    public override ActionResult Stop(Agent agent, float deltaTime)
    {
 	     return ActionResult.SUCCESS;
    }
}