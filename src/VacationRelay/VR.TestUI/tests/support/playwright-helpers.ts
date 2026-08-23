import { expect, Locator } from "@playwright/test";

export async function flash(button:Locator ): Promise<void>  {
    await button.scrollIntoViewIfNeeded();

    var res = await button.evaluate((el: HTMLElement) => {
    let i = 0;
    // enlarge and animate
    el.style.transition = 'all 0.15s ease';
    el.style.transform = 'scale(1.25)';
    el.style.padding = '12px 18px';
    el.style.fontSize = '1.05em';
    el.style.borderWidth = '2px';
    const colors = ['yellow', 'red', 'orange', 'white'];
    const interval = setInterval(() => {
      el.style.background = colors[i % colors.length];
      i++;
      if (i > 7) {
        clearInterval(interval);
        el.style.background = '';
      }
    }, 150);
  });
   await sleep(2);
   return res;
} 
export async function flashAndClick(button:Locator ) : Promise<void>{
  await flash(button);
  await sleep(2);
  return await button.click();  
  
}

export async function expectAndFlash(button:Locator ): Promise<void> {
  
  var res= await expect(button).toBeVisible();
  await flash(button);
  return res;
}

export async function sleep(seconds: number) : Promise<unknown> {
  var x= new Promise(resolve => setTimeout(resolve, seconds*1000));
  return x;
}
