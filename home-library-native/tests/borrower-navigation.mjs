export async function borrowerNavigate(page,tab){
 if(await page.evaluate(()=>document.body.dataset.account==='true'&&innerWidth<=900&&!document.body.classList.contains('shell-nav-open')))await page.locator('[data-shell-toggle]').click();
 await page.locator('[data-tab='+tab+']').click();
}
