using Microsoft.AspNetCore.Mvc;
[Area("User")]
public class HomeUserController:Controller{
    public ActionResult Index(){
        return View();
    }
    
}