import RAIN.Core;
import RAIN.Action;

@RAINAction
class ActionTemplate_JS extends RAIN.Action.RAINAction
{
	function newclass()
	{
		actionName = "ActionTemplate_JS";
	}
	
	function Start(ai:AI, deltaTime:float):void
	{
        super.Start(ai, deltaTime);
	}

	function Execute(ai:AI, deltaTime:float):ActionResult
	{
        return ActionResult.SUCCESS;
	}

   	function Stop(ai:AI, deltaTime:float):void
	{
        super.Stop(ai, deltaTime);
	}
}