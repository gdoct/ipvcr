/**
 * Login form end-to-end test
 * Tests the login functionality of the application
 */

/**
 * Main test function that will be executed by the test runner
 * @param {import('puppeteer').Browser} browser - Puppeteer browser instance
 * @param {Object} config - Test configuration
 */
async function run(browser, config) {
  // Create a new page
  const page = await browser.newPage();
  
  // Set viewport size
  await page.setViewport({ width: 1280, height: 800 });
  
  // Navigate to login page (modify this URL according to your app structure)
  const loginUrl = `${config.baseUrl}/login`;
  console.log(`Navigating to ${loginUrl}`);
  await page.goto(loginUrl, {
    waitUntil: 'networkidle2',
    timeout: config.timeout
  });
  
  // Take a screenshot of the login page
  await page.screenshot({ 
    path: `${config.screenshotsDir}/login-before.png`,
    fullPage: true 
  });
  
  // Wait for the login form to be available
  await page.waitForSelector('form', { timeout: config.timeout });
  
  // Fill out the login form using selectors that match the actual form
  // Looking at your LoginPage.tsx, the inputs are inside InputGroup components
  await page.type('input[type="text"]', 'admin');
  await page.type('input[type="password"]', 'ipvcr');
  
  // Submit the form by clicking the login button
  console.log('Submitting login form');
  await Promise.all([
    page.click('button[type="submit"]'),
    await page.waitForNavigation({ waitUntil: 'networkidle2', timeout: config.timeout })
  ]).catch(error => {
    throw new Error(`Failed to submit login form or navigation: ${error.message}`);
  });
  
  // Take a screenshot after login attempt
  await page.screenshot({ 
    path: `${config.screenshotsDir}/login-after.png`,
    fullPage: true 
  });
  
  // Verify successful login by checking for elements that should appear after login
  // Since the user navigates to /recordings after login, check for elements there
  try {
    // After successful login, we should be redirected to the recordings page
    // Wait for URL to contain 'recordings'
    await page.waitForFunction(
      () => window.location.pathname.includes('/recordings'),
      { timeout: config.timeout }
    );
    
    console.log('Successfully logged in - redirected to recordings page');
  } catch (error) {
    // Check if there's an error message displayed
    const errorElement = await page.$('.alert-danger');
    if (errorElement) {
      const errorText = await page.evaluate(el => el.textContent, errorElement);
      throw new Error(`Login failed with error: ${errorText}`);
    } else {
      throw new Error('Login verification failed - not redirected to recordings page');
    }
  }
  
  console.log('Login test passed!');
}

module.exports = { run };