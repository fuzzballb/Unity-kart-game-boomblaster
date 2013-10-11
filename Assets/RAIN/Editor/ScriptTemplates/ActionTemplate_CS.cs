using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RAIN.Core;
using RAIN.Action;

[RAINAction]
public class ActionTemplate_CS : RAINAction
{
    public ActionTemplate_CS()
    {
        actionName = "ActionTemplate_CS";
    }

    public override void Start(AI ai, float deltaTime)
    {
        base.Start(ai, deltaTime);
    }

    public override ActionResult Execute(AI ai, float deltaTime)
    {
        return ActionResult.SUCCESS;
    }

    public override void Stop(AI ai, float deltaTime)
    {
        base.Stop(ai, deltaTime);
    }
}