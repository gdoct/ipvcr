const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');

// Configuration
const config = {
  baseUrl: 'http://localhost:3000', // Adjust to your app's development URL
  headless: process.env.HEADLESS !== 'false', // Run headless by default
  slowMo: process.env.SLOW_MO ? parseInt(process.env.SLOW_MO) : 0, // Slow down execution for debugging
  screenshotsDir: path.join(__dirname, 'screenshots'),
  timeout: 30000
};

// Ensure screenshots directory exists
if (!fs.existsSync(config.screenshotsDir)) {
  fs.mkdirSync(config.screenshotsDir, { recursive: true });
}

// Track test results
let passed = 0;
let failed = 0;

// Helper to run a test file
async function runTest(testFile) {
  console.log(`\n🧪 Running test: ${path.basename(testFile)}`);
  
  const browser = await puppeteer.launch({ 
    headless: config.headless ? "new" : false,
    slowMo: config.slowMo,
    args: ['--no-sandbox', '--disable-setuid-sandbox']
  });
  
  try {
    // Load the test module
    const test = require(testFile);
    // Run the test
    await test.run(browser, config);
    console.log(`✅ Passed: ${path.basename(testFile)}`);
    passed++;
  } catch (error) {
    console.error(`❌ Failed: ${path.basename(testFile)}`);
    console.error(error);
    
    // Take screenshot on error
    try {
      const pages = await browser.pages();
      if (pages.length > 0) {
        const screenshotPath = path.join(
          config.screenshotsDir, 
          `error-${path.basename(testFile, '.js')}-${Date.now()}.png`
        );
        await pages[0].screenshot({ path: screenshotPath });
        console.log(`📸 Error screenshot saved to: ${screenshotPath}`);
      }
    } catch (screenshotError) {
      console.error('Failed to take error screenshot:', screenshotError);
    }
    
    failed++;
  } finally {
    await browser.close();
  }
}

// Main function to discover and run all tests
async function runTests() {
  console.log('🚀 Starting E2E tests with Puppeteer');
  
  // Get all test files (all .test.js files in the e2e directory)
  const testDir = __dirname;
  const testFiles = fs.readdirSync(testDir)
    .filter(file => file.endsWith('.test.js'))
    .map(file => path.join(testDir, file));
  
  if (testFiles.length === 0) {
    console.log('⚠️ No test files found. Create files with .test.js extension in the e2e directory.');
    return;
  }
  
  console.log(`Found ${testFiles.length} test files`);
  
  // Run each test sequentially
  for (const testFile of testFiles) {
    await runTest(testFile);
  }
  
  // Report results
  console.log('\n📊 Test Results:');
  console.log(`Total: ${testFiles.length} | ✅ Passed: ${passed} | ❌ Failed: ${failed}`);
  
  // Exit with appropriate code
  process.exit(failed > 0 ? 1 : 0);
}

// Run the tests
runTests().catch(error => {
  console.error('Error running tests:', error);
  process.exit(1);
});