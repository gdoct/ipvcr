/**
 * Recording creation end-to-end test
 * Tests the functionality to create a new recording
 */

/**
 * Helper function for login
 * @param {import('puppeteer').Page} page - Puppeteer page instance
 * @param {Object} config - Test configuration
 */
async function performLogin(page, config) {
  // Navigate to login page
  const loginUrl = `${config.baseUrl}/login`;
  console.log(`Navigating to ${loginUrl}`);
  await page.goto(loginUrl, {
    waitUntil: 'networkidle2',
    timeout: config.timeout
  });

  // Wait for the login form to be available
  await page.waitForSelector('form', { timeout: config.timeout });

  // Fill out the login form
  await page.type('input[type="text"]', 'admin');
  await page.type('input[type="password"]', 'ipvcr');

  // Submit the form by clicking the login button
  console.log('Submitting login form');
  await Promise.all([
    page.click('button[type="submit"]'),
    page.waitForNavigation({ waitUntil: 'networkidle2', timeout: config.timeout })
  ]).catch(error => {
    throw new Error(`Failed to submit login form or navigation: ${error.message}`);
  });

  // Verify successful login by checking redirect to recordings page
  try {
    await page.waitForFunction(
      () => window.location.pathname.includes('/recordings'),
      { timeout: config.timeout }
    );
    console.log('Successfully logged in - redirected to recordings page');
  } catch (error) {
    const errorElement = await page.$('.alert-danger');
    if (errorElement) {
      const errorText = await page.evaluate(el => el.textContent, errorElement);
      throw new Error(`Login failed with error: ${errorText}`);
    } else {
      throw new Error('Login verification failed - not redirected to recordings page');
    }
  }
}

/**
 * Generate a random recording name
 * @returns {string} A random recording name with timestamp
 */
function generateRandomRecordingName() {
  return `Test Recording ${new Date().toISOString().replace(/[:.]/g, '-')}`;
}

/**
 * Helper function to wait in older versions of Puppeteer that don't have waitForTimeout
 * @param {import('puppeteer').Page} page - Puppeteer page instance 
 * @param {number} ms - Time to wait in milliseconds
 * @returns {Promise<void>}
 */
async function waitFor(page, ms) {
  return new Promise(resolve => setTimeout(resolve, ms));
}

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

  // Step 1: Login first
  await performLogin(page, config);
  // console.log('Login successful, now navigating to recordings page');
  // await page.waitForNavigation({ waitUntil: 'networkidle2', timeout: config.timeout })
  const recordingButtonSelector = '[data-testid="add-recording-btn"]';
  console.log('Navigated to recordings page, finding button to add new recording');
  await page.waitForSelector(recordingButtonSelector, { timeout: config.timeout });
  const addRecordingButton = await page.$(recordingButtonSelector);
  if (!addRecordingButton) {
    throw new Error('Add recording button not found');
  }
  console.log('Found add recording button, clicking it');
  await page.click(recordingButtonSelector),
  console.log('Clicked on add recording button, waiting for navigation');
  await waitFor(page, 100);
  console.log('Navigated to new page ' + page.url());
  // Verify we're on the new recording page or a page for adding recordings

  // Step 3: Fill in the recording form
  const recordingName = generateRandomRecordingName();
  console.log(`Filling form with recording name: ${recordingName}`);

  // Wait for the form to be fully loaded and take a screenshot to help debug
  console.log('Waiting for form elements to be available...');
  await waitFor(page, 1000); // Allow time for form to render

  // Try to find the name input with multiple possible selectors
  console.log('Looking for recording name input field');
  // data-testid="recording-name-input"
  var recordingNameInputSelector = '[data-testid="recording-name-input"]';
  await page.$(recordingNameInputSelector);
  await page.type(recordingNameInputSelector, recordingName);


  // Start typing in the channel autocomplete with improved error handling
  var channelSelectInput = await page.$('[data-testid="channel-search-input"]');
  await page.type(channelSelectInput, 'VIA');

  // Wait for the dropdown with potential multiple updates
  console.log('Waiting for channel dropdown to stabilize...');

  // Add initial delay to allow dropdown time to appear and start updating
  await waitFor(page, 1500);

  // Try multiple selectors for the dropdown
  const dropdownSelector = '[data-testid="channel-dropdown"]'
  const dropdownItemSelector = 'data-test-group="channel-option"';
  const channeldropdown = await page.$(dropdownSelector);
  const items = channeldropdown.querySelectorAll(dropdownItemSelector);

        // Try to find an item containing "VIA"
  for (const item of items) {
    if (item.textContent && item.textContent.includes('VIA')) {
      item.click();
      return true;
    }
  }

// Step 4: Try to save the recording with multiple possible selectors
console.log('Looking for save recording button');
const saveButton = await page.$('[data-testid="save-recording-btn"]');
saveButton.click();
await waitFor(page, 1500);

// Try to verify we're back on the recordings list page
try {
  await page.waitForFunction(
    () => (page.url().indexOf('/recordings') > 0),
    { timeout: config.timeout }
  );
  console.log('Successfully returned to recordings list page');
} catch (error) {
  console.log('Warning: Not redirected to recordings page after saving, taking a screenshot');
  await page.screenshot({
    path: `${config.screenshotsDir}/recording-create-after-save.png`,
    fullPage: true
  });
  // Continue with the test anyway
}
await waitFor(page, 1500);
// Step 5: Verify the new recording is in the list
console.log(`Verifying recording "${recordingName}" appears in the list`);
const newItemSelector = 'data-recordingname=""' + recordingName + '"';
// Search for the recording name in the list
const recordingExists = await page.evaluate(newItemSelector);

if (!recordingExists) {
  throw new Error(`New recording "${recordingName}" not found in the recordings list`);
}

console.log('Recording creation test passed! New recording was successfully created and is visible in the list.');
}

module.exports = { run };